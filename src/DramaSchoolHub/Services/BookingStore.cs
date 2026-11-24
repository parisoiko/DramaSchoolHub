using DramaSchoolHub.Models;

namespace DramaSchoolHub.Services;

// Keeps the bookings in memory. Registered as a SINGLETON (step 2), so every user's
// request uses this one object, often at the same moment.
// MODULE 4, step 2: this works for one user at a time, but it isn't thread-safe yet.
public class BookingStore : IBookingStore
{
    private readonly List<Booking> _bookings = [];
    private int _nextId = 1;
    private readonly Lock _lock = new();

    public BookingStore()
    {
        // Sample bookings: one that has already ended (yesterday) and two coming up.
        DateTime today = DateTime.Today;
        Add(new Booking { RoomId = 2, BookedBy = "Year 1: Chekhov scene study", Start = today.AddDays(-1).AddHours(10), Hours = 2 });
        Add(new Booking { RoomId = 1, BookedBy = "Year 3: Hamlet run-through", Start = today.AddDays(1).AddHours(14), Hours = 4 });
        Add(new Booking { RoomId = 4, BookedBy = "Improv society", Start = today.AddDays(2).AddHours(18), Hours = 2 });
    }

    public Booking Add(Booking booking)
    {
        lock (_lock)
        {
            booking.Id = _nextId++;
            _bookings.Add(booking);
            return booking;
        }
    }

    // A copy, so the caller can loop over it while other requests add bookings.
    public List<Booking> GetAll()
    {
        lock (_lock)
        {
            return _bookings.ToList();
        }
    }

    public int RemoveEndedBefore(DateTime moment)
    {
        lock (_lock)
        {
            return _bookings.RemoveAll(booking => booking.End < moment);
        }
    }
}
