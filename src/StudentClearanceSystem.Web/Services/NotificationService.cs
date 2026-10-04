using StudentClearanceSystem.Web.Data;
using StudentClearanceSystem.Web.Models;

namespace StudentClearanceSystem.Web.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task NotifyAsync(string recipientUserId, string message)
    {
        _context.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Message = message,
            IsRead = false,
            CreatedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
