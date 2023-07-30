using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using System.Reflection;

namespace Logger.Infrastructure.Service.Log
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterLogServices(this IServiceCollection services, IConfiguration configuration, IHostBuilder host)
        {
            var _BaseUrlLog = new Uri(configuration["ElasticConfiguration:Uri"]).ToString();
            var _UsernameLog = configuration["ElasticConfiguration:username"];
            var _PasswordLog = configuration["ElasticConfiguration:password"];

            var ElasticConfig = new ElasticsearchSinkOptions(new Uri(_BaseUrlLog))
            {
                ModifyConnectionSettings = x => x.BasicAuthentication(_UsernameLog, _PasswordLog),
                MinimumLogEventLevel = LogEventLevel.Information,
                BatchAction = ElasticOpType.Create,
                TypeName = null,
                FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
                FormatStackTraceAsArray = true,
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                                      EmitEventFailureHandling.WriteToFailureSink |
                                                      EmitEventFailureHandling.RaiseCallback,
                FailureSink = new FileSink("D:/logs/failures.txt", new ElasticsearchJsonFormatter(), null)

            };

            Serilog.Log.Logger = new LoggerConfiguration()

                .WriteTo.Elasticsearch(ElasticConfig)

                .Enrich.FromLogContext()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .MinimumLevel.Override("System", LogEventLevel.Error)

                //.Enrich.WithProperty("ApplicationName", .HostingEnvironment.ApplicationName)
                //.Enrich.WithProperty("Environment", builder.HostingEnvironment.EnvironmentName)
                .Enrich.WithExceptionDetails()
                .Filter.ByExcluding("StartsWith(SourceContext, 'Microsoft.')")
                .Filter.ByExcluding("RequestPath like '/health%'")
                .Filter.ByExcluding("RequestPath like '/metrics-prometheus%'")
                .Filter.ByExcluding("RequestPath like '/docs%'")
                .Filter.ByExcluding("RequestPath like '/swagger%'")

                .CreateBootstrapLogger();

            host.UseSerilog();

            return services;
        }
    } 
}