using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Models;

public class Department
{
    public int DepartmentId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int FacultyId { get; set; }
    public Faculty? Faculty { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
