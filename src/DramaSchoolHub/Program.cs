using DramaSchoolHub.Extensions;
using DramaSchoolHub.Filters;
using DramaSchoolHub.Middleware;
using DramaSchoolHub.Models;
using DramaSchoolHub.Notifications;
using DramaSchoolHub.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----
builder.Services.AddControllersWithViews(options => options.Filters.Add<AuditFilter>());
builder.Services.AddDramaSchoolServices();
builder.Services.AddBookingRules(builder.Configuration);
builder.Services.AddNotificationSenders();

var app = builder.Build();

// ---- Middleware pipeline (order matters) ----
// First, so it times everything after it.
app.UseMiddleware<RequestTimingMiddleware>();

app.Use(async (context, next) =>
{
    // Headers must be set before the response starts, so before calling next.
    context.Response.Headers["X-Theatre"] = "DramaSchoolHub";
    await next(context);
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// Branches off before the box office, so health checks still answer when it's closed.
app.Map("/ping", branch => branch.Run(context => context.Response.WriteAsync("pong")));

// After static files, so the "closed" page still gets its CSS.
app.UseMiddleware<BoxOfficeMiddleware>();

app.UseRouting();

// ---- Endpoints ----
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var api = app.MapGroup("/api");

api.MapGet("/rooms", (IRoomCatalogue catalogue) => catalogue.GetAll());

api.MapGet("/rooms/{id:int}", (int id, IRoomCatalogue catalogue) =>
    catalogue.GetById(id) is RehearsalRoom room
        ? Results.Ok(room)
        : Results.NotFound());

api.MapGet("/bookings", (IBookingStore store) => store.GetAll());

// The senders only describe what they would send, so previewing is safe as a GET.
api.MapGet("/notifications/preview", (IEnumerable<INotificationSender> senders) =>
    senders.Select(sender => sender.Send("Studio B is double-booked")));

api.MapGet("/notifications/preview/urgent", ([FromKeyedServices("urgent")] INotificationSender sender) =>
    new[] { sender.Send("Studio B is double-booked") });

app.Run();

// Lets the integration tests start the app in memory (WebApplicationFactory<Program>).
public partial class Program { }
