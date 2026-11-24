using DramaSchoolHub.Controllers;
using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Tests;

// Unit tests: the controller gets a fake catalogue straight through its constructor, no container involved.
public class HomeControllerTests
{
    private static readonly IOptions<BookingRules> Rules = Options.Create(new BookingRules { MaxBookingHours = 3 });

    private static HomeController CreateController() =>
        new(new FakeRoomCatalogue(), NullLogger<HomeController>.Instance);

    [Fact]
    public void Index_ShowsTheCataloguesRooms()
    {
        IActionResult result = CreateController().Index();

        var view = Assert.IsType<ViewResult>(result);
        var rooms = Assert.IsType<List<RehearsalRoom>>(view.Model);
        Assert.Equal("Tiny Room", Assert.Single(rooms).Name);
    }

    [Fact]
    public void Room_ShowsTheRoomWithTheLongestBooking()
    {
        IActionResult result = CreateController().Room(1, Rules);

        var details = Assert.IsType<RoomDetails>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Tiny Room", details.Room.Name);
        Assert.Equal(3, details.MaxBookingHours);
    }

    [Fact]
    public void Room_UnknownId_IsNotFound()
    {
        Assert.IsType<NotFoundResult>(CreateController().Room(99, Rules));
    }

    private sealed class FakeRoomCatalogue : IRoomCatalogue
    {
        private readonly List<RehearsalRoom> _rooms = [new() { Id = 1, Name = "Tiny Room", Capacity = 2, HasPiano = false }];

        public List<RehearsalRoom> GetAll() => _rooms;

        public RehearsalRoom? GetById(int id) => _rooms.FirstOrDefault(room => room.Id == id);
    }
}
