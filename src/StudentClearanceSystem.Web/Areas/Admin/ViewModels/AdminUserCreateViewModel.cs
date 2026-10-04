using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class AdminUserCreateViewModel
{
    [Required, StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}
