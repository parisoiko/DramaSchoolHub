// DramaSchoolHub: rehearsal-room booking for a drama school.
// Built module by module while following "Applying Dependency Injection
// & Middleware in ASP.NET Core 10". See README.md for the plan.

using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ---- Services: everything the app can ask the DI container for ----
builder.Services.AddControllersWithViews();

// MODULE 1, step 3: register your own service here.
builder.Services.AddSingleton<IRoomCatalogue, RoomCatalogue>();
// MODULE 2, step 2: register the three stage passes, one per lifetime.
builder.Services.AddTransient<TransientPass>();
builder.Services.AddScoped<ScopedPass>();
builder.Services.AddSingleton<SingletonPass>();
// MODULE 2, step 5: bind the "BookingRules" section of appsettings.json.
builder.Services.AddOptions<BookingRules>()
    .Bind(builder.Configuration.GetSection("BookingRules"))
    .ValidateDataAnnotations()
    .Validate(rules => rules.OpeningHour < rules.ClosingHour, "OpeningHour must be before ClosingHour.")
    .ValidateOnStart();
// MODULE 4, step 3: replace the Configure line above with a validated version.

// MODULE 4, step 1: register the Usher (try Singleton first, on purpose).
builder.Services.AddScoped<Usher>();
// MODULE 4, step 2: register the booking store.
builder.Services.AddSingleton<IBookingStore, BookingStore>();
// MODULE 4, step 5: register the background service.
builder.Services.AddHostedService<BookingCleanupService>();

var app = builder.Build();

// ---- Middleware pipeline: what every request passes through ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
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

app.Run();

// Lets the tests start this app in memory (WebApplicationFactory<Program>).
public partial class Program { }
