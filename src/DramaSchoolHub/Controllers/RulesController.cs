using DramaSchoolHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Controllers;

public class RulesController(IOptionsSnapshot<BookingRules> options) : Controller
{
    public IActionResult Index() => View(options.Value);
}