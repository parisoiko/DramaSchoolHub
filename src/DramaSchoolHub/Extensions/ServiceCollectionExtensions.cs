using DramaSchoolHub.Models;
using DramaSchoolHub.Notifications;
using DramaSchoolHub.Services;

namespace DramaSchoolHub.Extensions;

// Keeps Program.cs short: each method registers one area of the app.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDramaSchoolServices(this IServiceCollection services)
    {
        // The rooms never change, so one shared, read-only catalogue is enough.
        services.AddSingleton<IRoomCatalogue, RoomCatalogue>();

        // Shared by every request at once, which is why BookingStore is thread-safe.
        services.AddSingleton<IBookingStore, BookingStore>();
        services.AddHostedService<BookingCleanupService>();

        // The lifetimes demo: one pass per lifetime, and an usher that takes the request's pass.
        services.AddTransient<TransientPass>();
        services.AddScoped<ScopedPass>();
        services.AddSingleton<SingletonPass>();
        services.AddScoped<Usher>();

        return services;
    }

    public static IServiceCollection AddBookingRules(this IServiceCollection services, IConfiguration configuration)
    {
        // Validated at start-up: a bad setting stops the app instead of reaching users.
        services.AddOptions<BookingRules>()
            .Bind(configuration.GetSection("BookingRules"))
            .ValidateDataAnnotations()
            .Validate(rules => rules.OpeningHour < rules.ClosingHour, "OpeningHour must be before ClosingHour.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddNotificationSenders(this IServiceCollection services)
    {
        // Both come back from IEnumerable<INotificationSender>...
        services.AddSingleton<INotificationSender, EmailSender>();
        services.AddSingleton<INotificationSender, SmsSender>();

        // ...while urgent messages ask for this one by key. Keyed registrations aren't in that list.
        services.AddKeyedSingleton<INotificationSender, SmsSender>("urgent");

        return services;
    }
}
