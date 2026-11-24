using DramaSchoolHub.Models;

namespace DramaSchoolHub.Services;

public interface IBookingStore
{
    Booking Add(Booking booking);
    List<Booking> GetAll();
    int RemoveEndedBefore(DateTime moment);
}
