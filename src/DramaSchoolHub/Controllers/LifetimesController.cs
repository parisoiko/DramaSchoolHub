using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace DramaSchoolHub.Controllers;

// Asks for each kind of pass twice: matching codes mean the container handed over the same object.
public class LifetimesController(
    TransientPass transient1, TransientPass transient2,
    ScopedPass scoped1, ScopedPass scoped2,
    SingletonPass singleton1, SingletonPass singleton2,
    Usher usher) : Controller
{
    public IActionResult Index()
    {
        var rows = new List<LifetimeRow>
        {
            new("Transient", transient1.Code, transient2.Code),
            new("Scoped", scoped1.Code, scoped2.Code),
            new("Singleton", singleton1.Code, singleton2.Code),
        };

        // A different service, but in the same request it receives the same ScopedPass.
        ViewData["UsherGreeting"] = usher.Greet();
        return View(rows);
    }
}
