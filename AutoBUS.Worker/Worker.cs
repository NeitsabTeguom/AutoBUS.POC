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
        private Broker broker;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            try
            {
                this.broker = new Broker(Broker.BrokerTypes.Worker);

                _logger = logger;
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
                this.broker.Start();
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
                this.broker.Stop();
            }
            catch { }
        }
    }
}
