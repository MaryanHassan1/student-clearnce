using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Models;

public class Notification
{
    public int NotificationId { get; set; }

    [Required]
    public string RecipientUserId { get; set; } = string.Empty;
    public ApplicationUser? RecipientUser { get; set; }

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }
}
