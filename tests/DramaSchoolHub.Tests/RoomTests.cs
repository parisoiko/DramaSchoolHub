using System.Net;
using System.Net.Http.Json;
using DramaSchoolHub.Controllers;
using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DramaSchoolHub.Tests;

// The room pages and the rooms API, through the real app running in memory.
public class RoomTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HomePage_ListsTheRooms()
    {
        string body = await factory.CreateClient().GetStringAsync("/");

        Assert.Contains("Main Stage", body);
        Assert.Contains("Black Box", body);
        Assert.Contains("href=\"/rooms/2\"", body);
    }

    [Fact]
    public async Task HomePage_UsesTheRegisteredCatalogue()
    {
        HttpClient client = factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IRoomCatalogue>(new OneRoomCatalogue())))
            .CreateClient();

        string body = await client.GetStringAsync("/");

        Assert.Contains("Test Room", body);
        Assert.DoesNotContain("Main Stage", body);
    }

    [Fact]
    public async Task HomePage_LogsTheRoomCount()
    {
        var logs = new CollectingLoggerProvider();
        await factory.WithLogs(logs).CreateClient().GetAsync("/");

        Assert.Contains("Showing 4 rehearsal rooms", logs.From<HomeController>());
    }

    [Fact]
    public async Task RoomPage_ShowsTheLongestBookingFromTheRules()
    {
        HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync("/rooms/2");
        Assert.Contains("Studio A", body);
        Assert.Contains("5 hours", body);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/rooms/99")).StatusCode);
    }

    [Fact]
    public async Task Footer_ShowsTheOpeningHoursFromConfiguration()
    {
        string body = await factory.CreateClient().GetStringAsync("/Lifetimes");
        Assert.Contains("Rooms open 09:00 to 22:00", body);

        string changed = await factory
            .WithSettings(new() { ["BookingRules:OpeningHour"] = "8" })
            .CreateClient().GetStringAsync("/");
        Assert.Contains("Rooms open 08:00 to 22:00", changed);
    }

    [Fact]
    public async Task Api_ListsAndFindsRooms()
    {
        HttpClient client = factory.CreateClient();

        List<RehearsalRoom>? rooms = await client.GetFromJsonAsync<List<RehearsalRoom>>("/api/rooms");
        Assert.Equal(4, rooms!.Count);

        RehearsalRoom? blackBox = await client.GetFromJsonAsync<RehearsalRoom>("/api/rooms/4");
        Assert.Equal("Black Box", blackBox!.Name);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/rooms/99")).StatusCode);
    }

    private sealed class OneRoomCatalogue : IRoomCatalogue
    {
        private readonly List<RehearsalRoom> _rooms = [new() { Id = 99, Name = "Test Room", Capacity = 5 }];

        public List<RehearsalRoom> GetAll() => _rooms;

        public RehearsalRoom? GetById(int id) => _rooms.FirstOrDefault(room => room.Id == id);
    }
}
