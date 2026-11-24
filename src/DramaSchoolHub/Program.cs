// DramaSchoolHub: rehearsal-room booking for a drama school.
// Built module by module while following "Applying Dependency Injection
// & Middleware in ASP.NET Core 10". See README.md for the plan.

using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using DramaSchoolHub.Middleware;
using Microsoft.AspNetCore.Mvc;
using DramaSchoolHub.Filters;
using DramaSchoolHub.Notifications;
using DramaSchoolHub.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---- Services: everything the app can ask the DI container for ----
builder.Services.AddControllersWithViews(options => options.Filters.Add<AuditFilter>());

// MODULE 6, step 2: the school's own services, registered in Extensions/ServiceCollectionExtensions.cs.
builder.Services.AddDramaSchoolServices();

// Booking rules stay here: they need builder.Configuration.
builder.Services.AddOptions<BookingRules>()
    .Bind(builder.Configuration.GetSection("BookingRules"))
    .ValidateDataAnnotations()
    .Validate(rules => rules.OpeningHour < rules.ClosingHour, "OpeningHour must be before ClosingHour.")
    .ValidateOnStart();

// MODULE 6, step 1: register BOTH notification senders.
builder.Services.AddSingleton<INotificationSender, EmailSender>();
builder.Services.AddSingleton<INotificationSender, SmsSender>();
// MODULE 6, step 3: register the SMS sender again, under the key "urgent".
builder.Services.AddKeyedSingleton<INotificationSender, SmsSender>("urgent");

var app = builder.Build();

// ---- Middleware pipeline: what every request passes through, IN THIS ORDER ----
// MODULE 5, step 2: the request timer (first, so it times everything below it).
app.UseMiddleware<RequestTimingMiddleware>();
// MODULE 5, step 1: an inline middleware that adds a header.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Theatre"] = "DramaSchoolHub";
    await next(context);
});
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// MODULE 5, step 4: a /ping branch (before the box office, so it always answers).
app.Map("/ping", branch => branch.Run(context => context.Response.WriteAsync("pong")));
// MODULE 5, step 3: the box office (after static files, so the CSS still loads).
app.UseMiddleware<BoxOfficeMiddleware>();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// MODULE 3, step 3: minimal API endpoints for the rooms.
app.MapGet("/api/rooms", (IRoomCatalogue catalogue) => catalogue.GetAll());

app.MapGet("/api/rooms/{id}", (int id, IRoomCatalogue catalogue) =>
    catalogue.GetById(id) is RehearsalRoom room
        ? Results.Ok(room)
        : Results.NotFound());

// Provided for Module 4 (not an exercise): see the bookings at /api/bookings.
// [FromServices] because IBookingStore isn't registered until step 2; without it,
// the app couldn't even start before then.
app.MapGet("/api/bookings", ([FromServices] IBookingStore store) => store.GetAll());

// Provided for Module 6: asks for EVERY registered sender and sends through each.
app.MapGet("/api/notify", (IEnumerable<INotificationSender> senders) =>
    senders.Select(sender => sender.Send("Studio B is double-booked")));

// MODULE 6, step 3: /api/notify/urgent, using only the "urgent" sender.
app.MapGet("/api/notify/urgent", ([FromKeyedServices("urgent")] INotificationSender sender) =>
    new[] { sender.Send("Studio B is double-booked") });

app.Run();

// Lets the tests start this app in memory (WebApplicationFactory<Program>).
public partial class Program { }
