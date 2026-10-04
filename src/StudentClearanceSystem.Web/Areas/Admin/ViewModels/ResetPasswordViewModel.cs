using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class ResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;
}
