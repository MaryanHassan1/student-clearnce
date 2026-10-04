using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Models;

public class Faculty
{
    public int FacultyId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
