using System.ComponentModel.DataAnnotations;

namespace DramaSchoolHub.Models;

public class BookingRules
{
    [Range(0, 23)]
    public int OpeningHour { get; set; }
    [Range(1, 24)]
    public int ClosingHour { get; set; }
    [Range(1, 5)]
    public int MaxBookingHours { get; set; }
    public bool BoxOfficeClosed { get; set; }
}