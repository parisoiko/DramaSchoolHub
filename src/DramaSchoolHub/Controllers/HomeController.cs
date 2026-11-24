using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Controllers;

public class HomeController : Controller
{
    private readonly IRoomCatalogue _catalogue;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IRoomCatalogue catalogue, ILogger<HomeController> logger)
    {
        _catalogue = catalogue;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var rooms = _catalogue.GetAll();
        _logger.LogInformation("Showing {RoomCount} rehearsal rooms", rooms.Count);
        return View(rooms);
    }

    public IActionResult Room(int id, [FromServices] IOptions<BookingRules> rules)
    {
        var room = _catalogue.GetById(id);
        if (room == null)
        {
            return NotFound();
        }
        return View(new RoomDetails(room, rules.Value.MaxBookingHours));
    }

    public IActionResult Error() => View();
}
