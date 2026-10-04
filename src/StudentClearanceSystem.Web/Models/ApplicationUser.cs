using Microsoft.AspNetCore.Identity;

namespace StudentClearanceSystem.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }

    public Student? Student { get; set; }
}
