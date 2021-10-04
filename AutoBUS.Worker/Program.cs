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


        // INSTALL
        // Win : https://anthonygiretti.com/2020/01/02/building-a-windows-service-with-worker-services-and-net-core-3-1-part-1-introduction/
        // Linux : https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Host.CreateDefaultBuilder(args)
                    .UseSystemd()
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<Worker>();
                    });
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

            // Just an executable for others
            return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            });
        }
    }
}
