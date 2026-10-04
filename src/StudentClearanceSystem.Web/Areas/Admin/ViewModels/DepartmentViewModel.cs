using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class DepartmentViewModel
{
    public int DepartmentId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, Display(Name = "Faculty")]
    public int FacultyId { get; set; }
}
