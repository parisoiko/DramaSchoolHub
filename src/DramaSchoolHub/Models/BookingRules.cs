using System.ComponentModel.DataAnnotations;

namespace DramaSchoolHub.Models;

// Bound from the "BookingRules" section of appsettings.json.
public class BookingRules
{
    [Range(0, 23)]
    public int OpeningHour { get; set; }

    [Range(1, 24)]
    public int ClosingHour { get; set; }

    [Range(1, 5)]
    public int MaxBookingHours { get; set; }

    // Turns every page away with a 503 (see BoxOfficeMiddleware); can be switched while the app runs.
    public bool BoxOfficeClosed { get; set; }
}
