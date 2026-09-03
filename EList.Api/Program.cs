using AutoMapper;
using EList.Api.Filtres;
using EList.Api.Infrastructure;
using EList.Api.Middleware;
using EList.AutoMapperProfile;
using EList.Common.DI;
using EList.DbDataProvider.Interfaces;
using EList.DI;
using EList.Services.Impl.BackgroundWorkers;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using ConfigurationManager = EList.Common.Configuration.ConfigurationManager;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager.Initialize(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "EList API",
            Description = "EList API"
        });

        // Assembly XML is EList.Api.xml (not EList.API.xml). Missing file used to 500 swagger.json on Linux.
        var xmlCommentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EList.Api.xml");
        if (File.Exists(xmlCommentsPath))
        {
            c.IncludeXmlComments(xmlCommentsPath, includeControllerXmlComments: true);
        }

        var apiSecurityScheme = AuthenticationSecuritySchemeFilter.GetOpenApiSecurityScheme();
        c.AddSecurityDefinition(apiSecurityScheme.Reference.Id, apiSecurityScheme);
        c.OperationFilter<AuthenticationSecuritySchemeFilter>();
        c.OperationFilter<HeaderFilter>();
    });
builder.Services.AddCors();
builder.Services.AddMvc();

builder.Services.AddAuthentication("BasicAuthentication").AddScheme<AuthenticationSchemeOptions, AuthenticationHandler>("BasicAuthentication", null);

ContainerConfigurator.Configure(builder.Services, new EListServiceMappingProvider());
var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new AutoMapperProfile()); });
var mapper = mappingConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

builder.Services.AddSingleton<DebtCollectorWorker>();
builder.Services.AddSingleton<IDebtCollectorUtility>(sp => sp.GetRequiredService<DebtCollectorWorker>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<DebtCollectorWorker>());

builder.Services.AddSingleton<OrganizationVerificationWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrganizationVerificationWorker>());

var app = builder.Build();

var pathBase = ConfigurationManager.AppSettings["pathBase"] ?? string.Empty;
app.UsePathBase(pathBase);
app.UseSwagger(c => { c.SerializeAsV2 = true; });
// Relative URL so Swagger UI works behind pathBase / reverse proxy without double-prefixing.
app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "EList API v1"); });
app.UseHttpsRedirection();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.UseRouting();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/ws") &&
        context.Request.Query.TryGetValue("token", out var token))
    {
        context.Request.Headers["Authorization"] = token.ToString();
    }
    await next();
});

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ClientInfoMiddleware>();
app.UseMiddleware<WebSocketsTokenHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ReConsentMiddleware>();

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 401 && !context.Response.HasStarted
        && context.Items.TryGetValue("AccountDisabledReason", out var reasonObj)
        && reasonObj is string reason)
    {
        context.Response.ContentType = "application/json";
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            errorCode = 20006,
            success = false,
            message = reason
        });
        await context.Response.WriteAsync(body);
    }
});

app.UseStaticFiles();

app.UseCors(cors =>
{
    var allowedOriginsRaw = ConfigurationManager.AppSettings.Contains("AllowedOrigins")
        ? ConfigurationManager.AppSettings["AllowedOrigins"]
        : string.Empty;

    var origins = (allowedOriginsRaw ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(o => o != "*")
        .ToArray();

    if (origins.Length > 0)
    {
        cors.WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("content-disposition");
    }
    else if (app.Environment.IsDevelopment())
    {
        // Dev fallback: open CORS only when AllowedOrigins is empty.
        cors.SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    }
    else
    {
        // Production without whitelist: deny cross-origin browser calls.
        cors.WithOrigins(Array.Empty<string>());
    }
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "EList.Api",
        version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        utc = DateTimeOffset.UtcNow
    }));
    endpoints.MapGet("/version", () => Results.Ok(new
    {
        service = "EList.Api",
        version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        environment = app.Environment.EnvironmentName,
        utc = DateTimeOffset.UtcNow
    }));
});
var minThreads = Convert.ToInt32(ConfigurationManager.AppSettings["minThreads"] ?? "0");
if (minThreads > 0)
{
    ThreadPool.GetMinThreads(out int _, out int minIOC);
    ThreadPool.SetMinThreads(minThreads, minIOC);
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto
});

// Configure DB before hosted background workers start processing.
using (var startupScope = app.Services.CreateScope())
{
    var dataConnectionProvider = startupScope.ServiceProvider.GetRequiredService<IDataConnectionProvider>();
    const string connectionStringName = "elist_main_db";
    dataConnectionProvider.Configure(connectionStringName);
}

app.Run();


