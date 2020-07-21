using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using Serilog;
using Serilog.Events;

namespace WebApplication
{
    public class HostBuilder
    {
        public static IHostBuilder Foundation<TStartUp>(string[] args) where TStartUp : class
        {

            return Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((context, configBuilder) =>
                 {
                     var builtConfig = configBuilder.Build();

                     if (builtConfig["appsettings"] != null)
                     {
                         Console.WriteLine("appsettings found in environment, importing config");

                         var memoryFileProvider = new InMemoryFileProvider(builtConfig["appsettings"]);
                         builtConfig = configBuilder.AddJsonFile(memoryFileProvider, "env_appsettings.json", false, false).Build();
                     }
                 })
                 .UseSerilog((hostBuilderContext, loggerConfiguration) =>
                 {
                     if (hostBuilderContext.Configuration.GetSection("datadog_api_key").Exists())
                     {
                         loggerConfiguration
                               .MinimumLevel.Debug()
                               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                               .Enrich.FromLogContext()
                               .Enrich.WithProperty("MachineName", Environment.MachineName)
                               .WriteTo.Console( outputTemplate: "{MachineName} | {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u4}] | {Message:l}{NewLine}{Exception}")
                               .WriteTo.DatadogLogs(hostBuilderContext.Configuration.GetSection("datadog_api_key").Value, "WorkshopWebApp", "WorkshopWebApp", Environment.MachineName);
                     }
                     else
                     {
                         loggerConfiguration.MinimumLevel.Debug()
                               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                               .Enrich.FromLogContext()
                               .Enrich.WithProperty("MachineName", Environment.MachineName)
                               .WriteTo.Console(outputTemplate: "{MachineName} | {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u4}] | {Message:l}{NewLine}{Exception}");


                     }
                 })
                 .ConfigureServices((services) =>
                 {
                     services.AddHttpClient();
                     services.AddHealthChecks().AddCheck("Health Check", () => HealthCheckResult.Healthy($"Application is running", GetData()), tags: new[] { "all" });
                 })
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<TStartUp>();
                 });
        }

        private static IReadOnlyDictionary<string, object> GetData()
        {
            return new Dictionary<string, object>() {
                { "Environment.MachineName" , Environment.MachineName.ToString() },
                { "Environment.OSVersion" , Environment.OSVersion.ToString() },
                { "Environment.ProcessorCount" , Environment.ProcessorCount.ToString() },
                { "Environment.WorkingSet" , Environment.WorkingSet.ToString() },
                { "DateTime.UtcNow" , DateTime.UtcNow.ToString("o") },
                { "DateTimeOffset.Now" , DateTimeOffset.Now.ToString("o") },
            };
        }
    }
}
