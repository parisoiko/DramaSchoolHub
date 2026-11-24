namespace DramaSchoolHub.Notifications;

// One way of telling staff about a problem. Several classes implement it (email, SMS).
public interface INotificationSender
{
    string Send(string message);
}
