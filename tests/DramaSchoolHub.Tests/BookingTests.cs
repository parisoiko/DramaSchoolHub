using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DramaSchoolHub.Tests;

// The singleton booking store (thread safety) and the background cleanup service.
public class BookingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void Store_IsOneSharedSingleton()
    {
        using IServiceScope request1 = factory.Services.CreateScope();
        using IServiceScope request2 = factory.Services.CreateScope();

        Assert.Same(
            request1.ServiceProvider.GetRequiredService<IBookingStore>(),
            request2.ServiceProvider.GetRequiredService<IBookingStore>());
    }

    [Fact]
    public void Store_SurvivesManyRequestsAtOnce()
    {
        var store = new BookingStore();
        int before = store.GetAll().Count;
        const int bookings = 20_000;

        // Half the "requests" add a booking while the other half read the list.
        Parallel.For(0, bookings * 2, i =>
        {
            if (i % 2 == 0)
                store.Add(new Booking { RoomId = 3, BookedBy = $"Student {i}", Start = DateTime.Today.AddDays(3), Hours = 1 });
            else
                store.GetAll();
        });

        List<Booking> all = store.GetAll();
        Assert.Equal(before + bookings, all.Count);
        Assert.Equal(all.Count, all.Select(booking => booking.Id).Distinct().Count());
    }

    [Fact]
    public void Store_RemovesOnlyEndedBookings()
    {
        var store = new BookingStore();

        int removed = store.RemoveEndedBefore(DateTime.Now);

        Assert.Equal(1, removed);
        Assert.DoesNotContain(store.GetAll(), booking => booking.BookedBy.Contains("Chekhov"));
        Assert.Equal(2, store.GetAll().Count);
    }

    [Fact]
    public async Task CleanupService_RemovesYesterdaysBookingAtStartUp()
    {
        Assert.Contains(factory.Services.GetServices<IHostedService>(), service => service is BookingCleanupService);

        HttpClient client = factory.CreateClient();
        string body = "";
        for (int attempt = 0; attempt < 50; attempt++)
        {
            body = await client.GetStringAsync("/api/bookings");
            if (!body.Contains("Chekhov")) break;
            await Task.Delay(100);
        }

        Assert.DoesNotContain("Chekhov", body);
        Assert.Contains("Hamlet", body);
    }

    [Fact]
    public async Task BookingsPage_ListsUpcomingBookingsWithRoomNames()
    {
        string body = await factory.CreateClient().GetStringAsync("/Bookings");

        Assert.Contains("Year 3: Hamlet run-through", body);
        Assert.Contains("Main Stage", body);
        Assert.Contains("Improv society", body);
    }
}
