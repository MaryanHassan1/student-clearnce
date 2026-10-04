using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Areas.Admin.ViewModels;

public class FacultyViewModel
{
    public int FacultyId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
}
