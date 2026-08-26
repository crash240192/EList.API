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
app.UseStaticFiles();


app.UseCors(builder => builder.SetIsOriginAllowed(origin => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
//app.UseCors(builder =>
//{
//    if (ConfigurationManager.AppSettings.Contains("AllowedOrigins") &&
//        ConfigurationManager.AppSettings["AllowedOrigins"].Length > 0 &&
//        ConfigurationManager.AppSettings["AllowedOrigins"] != "*")
//    {
//        builder.WithOrigins(ConfigurationManager.AppSettings["AllowedOrigins"].Split(",").Select(x => x.Trim()).ToArray());
//    }
//    else
//    {
//        builder.AllowAnyOrigin();
//    }
//    builder.AllowAnyMethod()
//        .AllowAnyHeader()
//        .WithExposedHeaders("content-disposition");
//});
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
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


