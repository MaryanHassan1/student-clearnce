namespace StudentClearanceSystem.Web.Services;

public interface INotificationService
{
    Task NotifyAsync(string recipientUserId, string message);
}
