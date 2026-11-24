namespace DramaSchoolHub.Models;

// One room booking. End is worked out from Start and Hours, so it can never disagree with them.
public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string BookedBy { get; set; } = "";
    public DateTime Start { get; set; }
    public int Hours { get; set; }
    public DateTime End => Start.AddHours(Hours);
}
