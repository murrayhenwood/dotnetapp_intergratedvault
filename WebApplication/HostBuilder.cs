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

                     if (builtConfig["vaultAddress"] != null)
                     {
                         Console.WriteLine("Vault address found in environment, importing config");

                         var vaultFileProvider = new VaultFileProvider(
                             vaultAddress: builtConfig["vaultAddress"],
                             vaultToken: builtConfig["vaultToken"],
                             secretPath: builtConfig["secretPath"],
                             providerPath: builtConfig["providerPath"]);

                         builtConfig = configBuilder.AddJsonFile(vaultFileProvider, "vault_appsettings.json", false, false).Build();
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
                     services.AddHealthChecks().AddCheck("Health Check", () => HealthCheckResult.Healthy($"Application is running", GetData(services)), tags: new[] { "all" });
                 })
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<TStartUp>();
                 });
        }

        private static IReadOnlyDictionary<string, object> GetData(IServiceCollection services)
        {
            return new Dictionary<string, object>() {
                { "Environment.MachineName" , Environment.MachineName.ToString() },
                { "Environment.OSVersion" , Environment.OSVersion.ToString() },
                { "Environment.ProcessorCount" , Environment.ProcessorCount.ToString() },
                { "Environment.WorkingSet" , Environment.WorkingSet.ToString() },
                { "DateTime.UtcNow" , DateTime.UtcNow.ToString("o") },
                { "DateTimeOffset.Now" , DateTimeOffset.Now.ToString("o") },
                { "Message" , services.BuildServiceProvider().GetService<HostBuilderContext>().Configuration.GetSection("message").Value},
            };
        }
    }
}
