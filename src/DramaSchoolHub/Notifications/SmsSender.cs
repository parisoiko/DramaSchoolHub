namespace DramaSchoolHub.Notifications;

public class SmsSender : INotificationSender
{
    public string Send(string message) => $"SMS to the duty manager: {message}";
}