namespace BIA.Middleware
{
    public class MemoryMonitorService : BackgroundService
    {
        private readonly ILogger<MemoryMonitorService> _logger;

        public MemoryMonitorService(ILogger<MemoryMonitorService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var memoryUsedMB = GC.GetTotalMemory(false) / (1024 * 1024);
                _logger.LogInformation($"Memory in use: {memoryUsedMB} MB");

                if (memoryUsedMB > 100) // if > 1GB
                {
                    _logger.LogWarning("High memory usage detected, forcing GC...");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

}
