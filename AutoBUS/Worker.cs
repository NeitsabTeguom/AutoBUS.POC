using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBUS
{
    public class Worker : BackgroundService
    {

        private SocketMiddleware sm = null;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            this.sm = new SocketMiddleware(SocketMiddleware.SocketType.Server, 11000);
            this.sm.Start();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
            
            if(stoppingToken.IsCancellationRequested)
            {
                this.sm.Stop();
            }
        }
    }
}
