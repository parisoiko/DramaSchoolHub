namespace DramaSchoolHub.Services;

public class BookingCleanupService(IServiceScopeFactory scopeFactory, ILogger<BookingCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            using IServiceScope scope = scopeFactory.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IBookingStore>();
            int removed = store.RemoveEndedBefore(DateTime.Now);
            logger.LogInformation("Removed {Count} ended bookings", removed);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}