using System.ComponentModel.DataAnnotations;
using StudentClearanceSystem.Web.Models.Enums;

namespace StudentClearanceSystem.Web.Models;

public class Student
{
    public int StudentId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, StringLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [Required, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public int FacultyId { get; set; }
    public Faculty? Faculty { get; set; }

    [Required]
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    [Required, StringLength(150)]
    public string Program { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ProfilePicturePath { get; set; }

    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

    public ICollection<ClearanceRequest> ClearanceRequests { get; set; } = new List<ClearanceRequest>();
}
