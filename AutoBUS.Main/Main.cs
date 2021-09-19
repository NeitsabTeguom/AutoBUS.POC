using AutoBUS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUSMain
{
    public class Main : BackgroundService
    {
        private Config<Config.ServiceConfigMain> config;

        private SocketMiddleware sm = null;

        private readonly ILogger<Main> _logger;

        public Main(ILogger<Main> logger)
        {
            this.config = new Config<Config.ServiceConfigMain>();

            _logger = logger;
            this.sm = new SocketMiddleware(
                SocketMiddleware.SocketType.Server, 
                this.config.sc.Broker.Port,
                checkInterval: this.config.sc.Broker.CheckInterval);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
            }
            
            if(stoppingToken.IsCancellationRequested)
            {
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Main starting...");
                this.sm.Start();
            }
            catch { }

            return base.StartAsync(cancellationToken);
            //return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.Stop();

            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            this.Stop();
            base.Dispose();
        }

        private void Stop()
        {
            try
            {
                _logger.LogInformation("Main stopping...");
                this.sm.Stop();
            }
            catch { }
        }
    }
}
