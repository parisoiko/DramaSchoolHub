using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Controllers;

public class HomeController(IRoomCatalogue catalogue, ILogger<HomeController> logger) : Controller
{
    public IActionResult Index()
    {
        var rooms = catalogue.GetAll();
        logger.LogInformation("Showing {RoomCount} rehearsal rooms", rooms.Count);
        return View(rooms);
    }

    // Only this action needs the rules, so they're injected here rather than in the constructor.
    [HttpGet("/rooms/{id:int}")]
    public IActionResult Room(int id, [FromServices] IOptions<BookingRules> rules)
    {
        var room = catalogue.GetById(id);
        if (room == null)
        {
            return NotFound();
        }

        return View(new RoomDetails(room, rules.Value.MaxBookingHours));
    }

    public IActionResult Error() => View();
}
