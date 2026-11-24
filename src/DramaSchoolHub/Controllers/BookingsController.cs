using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace DramaSchoolHub.Controllers;

public class BookingsController(IBookingStore store, IRoomCatalogue catalogue) : Controller
{
    public IActionResult Index()
    {
        var rows = store.GetAll()
            .OrderBy(booking => booking.Start)
            .Select(booking => new BookingRow(
                catalogue.GetById(booking.RoomId)?.Name ?? $"Room {booking.RoomId}",
                booking.BookedBy,
                booking.Start,
                booking.End))
            .ToList();

        return View(rows);
    }
}
