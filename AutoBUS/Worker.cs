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

        private HttpWebServer ws = null;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            this.ws = new HttpWebServer(new string[] { "http://localhost:8080/" });
            this.ws.Run();
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
