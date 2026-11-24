namespace DramaSchoolHub.Models;

// What the room details page needs: the room itself plus the longest booking allowed.
public record RoomDetails(RehearsalRoom Room, int MaxBookingHours);
