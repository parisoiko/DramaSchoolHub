using DramaSchoolHub.Services;

namespace DramaSchoolHub.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDramaSchoolServices(this IServiceCollection services)
    {
        services.AddSingleton<IRoomCatalogue, RoomCatalogue>();
        services.AddTransient<TransientPass>();
        services.AddScoped<ScopedPass>();
        services.AddSingleton<SingletonPass>();
        services.AddScoped<Usher>();
        services.AddSingleton<IBookingStore, BookingStore>();
        services.AddHostedService<BookingCleanupService>();
        return services;
    }
}