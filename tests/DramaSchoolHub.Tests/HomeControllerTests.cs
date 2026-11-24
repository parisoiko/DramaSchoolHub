using DramaSchoolHub.Controllers;
using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DramaSchoolHub.Tests;

// a) The fake: a second implementation of your interface.
public class FakeRoomCatalogue : IRoomCatalogue
{
    public List<RehearsalRoom> GetAll() =>
    [
        new RehearsalRoom { Id = 1, Name = "Tiny Room", Capacity = 2, HasPiano = false }
    ];

    // The interface requires it; no test asks the fake for one room yet.
    public RehearsalRoom? GetById(int id) => null;
}

public class HomeControllerTests
{
    [Fact]
    public void Module01_Step5_IndexShowsTheCataloguesRooms()
    {
        // Arrange
        var controller = new HomeController(new FakeRoomCatalogue(),NullLogger<HomeController>.Instance);

        // Act
        IActionResult result = controller.Index();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var rooms = Assert.IsType<List<RehearsalRoom>>(view.Model);
        Assert.Single(rooms);
        Assert.Equal("Tiny Room", rooms[0].Name);
    }
}