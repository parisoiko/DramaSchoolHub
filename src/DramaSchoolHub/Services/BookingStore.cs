using DramaSchoolHub.Models;

namespace DramaSchoolHub.Services;

// In-memory bookings. Registered as a singleton, so many requests use it at the same moment:
// every method takes the same lock, reads included (ToList while another thread adds would throw).
public class BookingStore : IBookingStore
{
    private readonly List<Booking> _bookings = [];
    private readonly Lock _lock = new();
    private int _nextId = 1;

    public BookingStore()
    {
        // Sample data: one booking that has already ended (the cleanup service removes it) and two to come.
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

    // Returns a copy, so callers can loop over it outside the lock.
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
