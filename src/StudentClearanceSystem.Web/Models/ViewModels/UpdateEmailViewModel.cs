using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Models.ViewModels;

public class UpdateEmailViewModel
{
    [Required, EmailAddress, StringLength(150)]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;
}
