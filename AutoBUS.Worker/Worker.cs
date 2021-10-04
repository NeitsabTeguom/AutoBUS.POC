using AutoBUS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUSWorker
{
    public class Worker : BackgroundService
    {
        private Config<Config.ServiceConfigWorker> config;

        private SocketMiddleware sm = null;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            try
            {
                this.config = new Config<Config.ServiceConfigWorker>();

                _logger = logger;
                this.sm = new SocketMiddleware(
                    SocketMiddleware.SocketType.Client,
                    this.config.sc.Broker.Port,
                    host: this.config.sc.Broker.Host,
                    checkInterval: this.config.sc.Broker.CheckInterval);
            }
            catch { }
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }

            if (stoppingToken.IsCancellationRequested)
            {
            }
            
            throw new NotImplementedException();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Worker starting...");
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
                _logger.LogInformation("Worker stopping...");
                this.sm.Stop();
            }
            catch { }
        }
    }
}
