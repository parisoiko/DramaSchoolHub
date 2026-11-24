using System.Net;
using System.Text.RegularExpressions;
using DramaSchoolHub.Filters;
using DramaSchoolHub.Middleware;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DramaSchoolHub.Tests;

// The custom middleware, its order in the pipeline, the /ping branch and the MVC audit filter.
public class PipelineTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient ClientWithBoxOffice(bool closed) => factory
        .WithSettings(new() { ["BookingRules:BoxOfficeClosed"] = closed ? "true" : "false" })
        .CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/api/rooms")]
    [InlineData("/css/site.css")]
    public async Task EveryResponse_CarriesTheTheatreHeader(string path)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(path);

        Assert.Equal("DramaSchoolHub", response.Headers.GetValues("X-Theatre").Single());
    }

    [Fact]
    public async Task RequestTimer_LogsEachRequestInMilliseconds()
    {
        var logs = new CollectingLoggerProvider();
        await factory.WithLogs(logs).CreateClient().GetAsync("/Lifetimes");

        Assert.Contains(logs.From<RequestTimingMiddleware>(), text => Regex.IsMatch(text, @"^GET /Lifetimes took \d+ ms$"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/rooms")]
    public async Task ClosedBoxOffice_TurnsVisitorsAway(string path)
    {
        HttpResponseMessage response = await ClientWithBoxOffice(closed: true).GetAsync(path);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("closed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ClosedBoxOffice_StillServesStaticFiles()
    {
        // The box office comes after UseStaticFiles, so the CSS never reaches it.
        HttpResponseMessage css = await ClientWithBoxOffice(closed: true).GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ping_AnswersWhetherTheBoxOfficeIsOpenOrClosed(bool closed)
    {
        string body = await ClientWithBoxOffice(closed).GetStringAsync("/ping");

        Assert.Equal("pong", body);
    }

    [Fact]
    public async Task AuditFilter_LogsMvcActionsWithTheirArgumentsOnly()
    {
        var logs = new CollectingLoggerProvider();
        HttpClient client = factory.WithLogs(logs).CreateClient();

        await client.GetAsync("/rooms/2");
        await client.GetAsync("/api/rooms");

        // Once, for the MVC action: filters don't run for minimal API endpoints.
        string entry = Assert.Single(logs.From<AuditFilter>());
        Assert.Contains("HomeController.Room", entry);
        Assert.EndsWith("with id=2", entry);
    }
}
