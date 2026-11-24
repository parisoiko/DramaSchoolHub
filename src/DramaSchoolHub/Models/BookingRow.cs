namespace DramaSchoolHub.Models;

// A booking as the Bookings page shows it: with the room's name instead of its id.
public record BookingRow(string RoomName, string BookedBy, DateTime Start, DateTime End);
