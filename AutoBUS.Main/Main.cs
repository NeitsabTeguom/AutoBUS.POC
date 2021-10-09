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
        private Broker broker;

        private readonly ILogger<Main> _logger;

        public Main(ILogger<Main> logger)
        {
            this.broker = new Broker(Broker.BrokerTypes.Federator);

            _logger = logger;
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
                _logger.LogInformation("Main starting...");
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
                _logger.LogInformation("Main stopping...");
                this.broker.Stop();
            }
            catch { }
        }
    }
}
