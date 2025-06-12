using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    /// <summary>
    /// A no-operation background service that can be used as a placeholder
    /// when we want to disable actual background services in certain environments
    /// </summary>
    public class NoOpBackgroundService : BackgroundService
    {
        private readonly ILogger<NoOpBackgroundService> _logger;

        public NoOpBackgroundService(ILogger<NoOpBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NoOpBackgroundService is running - background services are disabled");
            return Task.CompletedTask;
        }
    }
} 