namespace DramaSchoolHub.Notifications;

// Pretend sender: returns what it would send instead of really sending a text message.
public class SmsSender : INotificationSender
{
    public string Send(string message) => $"SMS to the duty manager: {message}";
}
