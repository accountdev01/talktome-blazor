using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.IService;

namespace TalkToMe.Shared.Services
{
    public class AutomatedSystem : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<AutomatedSystem> _logger;

        private readonly TimeSpan _period = TimeSpan.FromHours(24);

        public AutomatedSystem(IServiceProvider provider, ILogger<AutomatedSystem> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellation)
        {
            _logger.LogInformation("AutomatedSystem is starting.");

            using PeriodicTimer timer = new(_period);

            try
            {
                await DoWork();

                while (await timer.WaitForNextTickAsync(cancellation))
                {
                    await DoWork();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AutomatedSystem is stopping due to cancellation.");
            }
        }

        private async Task DoWork()
        {
            try
            {
                _logger.LogInformation("Executing automated work at: {time}", DateTimeOffset.Now);

                using var scope = _provider.CreateScope();
                var sp = scope.ServiceProvider;

                _logger.LogInformation("Work completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while performing automated work.");
            }
        }
    }
}