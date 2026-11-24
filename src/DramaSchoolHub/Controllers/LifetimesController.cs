using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace DramaSchoolHub.Controllers;

public class LifetimesController(
    TransientPass transient1, TransientPass transient2,
    ScopedPass scoped1, ScopedPass scoped2,
    SingletonPass singleton1, SingletonPass singleton2) : Controller
{
    public IActionResult Index()
    {
        var rows = new List<LifetimeRow>
        {
            new("Transient", transient1.Code, transient2.Code),
            new("Scoped", scoped1.Code, scoped2.Code),
            new("Singleton", singleton1.Code, singleton2.Code),
        };
        return View(rows);
    }
}