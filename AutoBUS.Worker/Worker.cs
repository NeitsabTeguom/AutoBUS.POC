using AutoBUS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUSWorker
{
    public class Worker : BackgroundService
    {
        private Config<Config.ServiceConfigWorker> config;

        private System.Timers.Timer t;

        private SocketMiddleware sm = null;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            this.config = new Config<Config.ServiceConfigWorker>();

            _logger = logger;
            this.sm = new SocketMiddleware(SocketMiddleware.SocketType.Client, this.config.sc.Broker.Port, this.config.sc.Broker.Host);
            this.sm.Start();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /*
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
            */
        }
    }
}
