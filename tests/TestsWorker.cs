using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kothf.Logging.File.Tests;

internal sealed class TestsWorker : BackgroundService
{
    private readonly ILogger<TestsWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private int _workerCount = 0;

    public TestsWorker(ILogger<TestsWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running tests...{workerCount}", ++_workerCount);

                await Task.Delay(100, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Tests canceled, Ready to exit ...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during testing!\n{exception}", ex.ToString());
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
