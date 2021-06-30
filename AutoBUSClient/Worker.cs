using AutoBUS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUSClient
{
    public class Worker : BackgroundService
    {
        private System.Timers.Timer t;

        private SocketMiddleware sm = null;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            this.sm = new SocketMiddleware(SocketMiddleware.SocketType.Client, 11000, null);
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
