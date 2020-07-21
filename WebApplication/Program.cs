using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostBuilder.Foundation<Startup>(args).Build().Run();
        }
    }
}
