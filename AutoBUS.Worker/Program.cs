using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace AutoBUSWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // https://levelup.gitconnected.com/net-core-worker-service-as-windows-service-or-linux-daemons-a9579a540b77
        // https://docs.microsoft.com/fr-fr/dotnet/core/extensions/windows-service

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Host.CreateDefaultBuilder(args)
                    .UseSystemd()
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<Worker>();
                    });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Host.CreateDefaultBuilder(args)
                .UseWindowsService((options) =>
                {
                    options.ServiceName = "AutoBUS Worker service";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
            }
            return null;
        }
    }
}
