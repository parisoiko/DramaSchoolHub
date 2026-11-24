namespace DramaSchoolHub.Notifications;

// Pretend sender: returns what it would send instead of really emailing.
public class EmailSender : INotificationSender
{
    public string Send(string message) => $"Email to the office: {message}";
}
