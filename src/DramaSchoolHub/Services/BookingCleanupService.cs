namespace DramaSchoolHub.Services;

// Removes ended bookings at start-up and then once a minute.
public class BookingCleanupService(IServiceScopeFactory scopeFactory, ILogger<BookingCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            // A hosted service is a singleton running outside any request, so it creates its own scope.
            // The store is a singleton today, but this keeps working if it becomes a scoped DbContext.
            using IServiceScope scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IBookingStore>();
            int removed = store.RemoveEndedBefore(DateTime.Now);
            logger.LogInformation("Removed {Count} ended bookings", removed);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
