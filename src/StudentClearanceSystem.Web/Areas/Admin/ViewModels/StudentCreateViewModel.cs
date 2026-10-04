using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using StudentClearanceSystem.Web.Models.Enums;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class StudentCreateViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "Registration Number")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, Display(Name = "Faculty")]
    public int FacultyId { get; set; }

    [Required, Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required, StringLength(150)]
    public string Program { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Academic Year")]
    public string AcademicYear { get; set; } = string.Empty;

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePicture { get; set; }

    [Required, StringLength(100, MinimumLength = 6)]
    [Display(Name = "Initial Password")]
    public string Password { get; set; } = string.Empty;
}
