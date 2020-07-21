using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApplication
{
    public class HealthCheckResponseWriter
    {
        public static Task TextFormatter(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "text/plain";

            StringBuilder sb = new StringBuilder();

            foreach (var item in result.Entries.Values)
            {
                List<string> vs = new List<string>();
                foreach (var pair in item.Data.AsEnumerable())
                {
                    vs.Add($"{pair.Key}:{pair.Value}");
                }
                sb.AppendLine("---------------------------------------------------------------------------------------------------------------------");
                sb.AppendLine($"        Status: {item.Status.ToString()}");
                sb.AppendLine($"   Description: {item.Description}");
                sb.AppendLine($"          Data: {String.Join("\n\t\t", vs)}");
                sb.AppendLine("---------------------------------------------------------------------------------------------------------------------");
            }

            return httpContext.Response.WriteAsync(sb.ToString());
        }

        public static Task JsonFormatter(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));
            return httpContext.Response.WriteAsync(
                json.ToString(Formatting.Indented));
        }
    }
}
