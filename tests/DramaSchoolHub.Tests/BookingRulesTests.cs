using DramaSchoolHub.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Tests;

// The Options pattern: binding, start-up validation and the rules page.
public class BookingRulesTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void Rules_AreBoundFromAppSettings()
    {
        BookingRules rules = factory.Services.GetRequiredService<IOptions<BookingRules>>().Value;

        Assert.Equal(9, rules.OpeningHour);
        Assert.Equal(22, rules.ClosingHour);
        Assert.Equal(5, rules.MaxBookingHours);
        Assert.False(rules.BoxOfficeClosed);
    }

    [Theory]
    [InlineData("BookingRules:MaxBookingHours", "0", "MaxBookingHours")]
    [InlineData("BookingRules:MaxBookingHours", "6", "MaxBookingHours")]
    [InlineData("BookingRules:OpeningHour", "23", "OpeningHour must be before ClosingHour")]
    public void InvalidRules_StopTheAppAtStartUp(string setting, string value, string expectedError)
    {
        string error = factory.WithSettings(new() { [setting] = value }).StartupError();

        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void ValidRules_LetTheAppStart()
    {
        Assert.Equal("", factory.WithSettings([]).StartupError());
    }

    [Fact]
    public async Task RulesPage_FollowsTheConfiguration()
    {
        string body = await factory.CreateClient().GetStringAsync("/Rules");
        Assert.Contains("09:00 to 22:00", body);
        Assert.Contains("up to 5 hours", body);

        string changed = await factory
            .WithSettings(new() { ["BookingRules:MaxBookingHours"] = "4" })
            .CreateClient().GetStringAsync("/Rules");
        Assert.Contains("up to 4 hours", changed);
    }
}
