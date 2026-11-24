using System.Net.Http.Json;
using DramaSchoolHub.Extensions;
using DramaSchoolHub.Notifications;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DramaSchoolHub.Tests;

// Several implementations of one interface, and a keyed registration.
public class NotificationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Preview_SendsThroughEveryRegisteredSender()
    {
        List<string>? sent = await factory.CreateClient().GetFromJsonAsync<List<string>>("/api/notifications/preview");

        Assert.Collection(sent!,
            email => Assert.StartsWith("Email", email),
            sms => Assert.StartsWith("SMS", sms));
    }

    [Fact]
    public async Task UrgentPreview_UsesOnlyTheKeyedSmsSender()
    {
        List<string>? sent = await factory.CreateClient().GetFromJsonAsync<List<string>>("/api/notifications/preview/urgent");

        Assert.StartsWith("SMS", Assert.Single(sent!));
    }

    [Fact]
    public void AddNotificationSenders_KeepsKeyedSendersOutOfTheList()
    {
        using ServiceProvider provider = new ServiceCollection().AddNotificationSenders().BuildServiceProvider();

        Assert.Equal(2, provider.GetServices<INotificationSender>().Count());
        Assert.IsType<SmsSender>(provider.GetRequiredKeyedService<INotificationSender>("urgent"));
        // Without a key, the last registration wins.
        Assert.IsType<SmsSender>(provider.GetRequiredService<INotificationSender>());
    }
}
