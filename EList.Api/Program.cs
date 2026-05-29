using AutoMapper;
using EList.Api.Filtres;
using EList.Api.Infrastructure;
using EList.Api.Middleware;
using EList.AutoMapperProfile;
using EList.Common.DI;
using EList.DbDataProvider.Interfaces;
using EList.DI;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using ConfigurationManager = EList.Common.Configuration.ConfigurationManager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<WebSocketConnectionManager>();

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

        //var xmlCommentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EList.API.xml");
        //c.IncludeXmlComments(xmlCommentsPath);

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

var app = builder.Build();

var pathBase = ConfigurationManager.AppSettings["pathBase"] ?? string.Empty;
app.UsePathBase(pathBase);
app.UseSwagger(c => { c.SerializeAsV2 = true; });
app.UseSwaggerUI(c => { c.SwaggerEndpoint($"{pathBase}/swagger/v1/swagger.json", "EList API v1"); });
app.UseHttpsRedirection();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.UseRouting();
app.UseMiddleware<ErrorHandlingMiddleware>();
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

//app.UseDeveloperExceptionPage();

IHostApplicationLifetime hostApplicationLifetime = app.Lifetime;
var scope = app.Services.CreateScope();

var dataConnectionProvider = scope.ServiceProvider.GetRequiredService<IDataConnectionProvider>();
var debtCollector = scope.ServiceProvider.GetRequiredService<IDebtCollectorUtility>();

hostApplicationLifetime.ApplicationStarted.Register(() =>
{
    const string connectionStringName = "elist_main_db";
    dataConnectionProvider.Configure(connectionStringName);
    debtCollector.Start();
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    debtCollector.Stop();
});

app.Run();


