using System.ComponentModel.DataAnnotations;

namespace StudentClearanceSystem.Web.Models;

public class ClearanceDepartment
{
    public int ClearanceDepartmentId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ClearanceItem> ClearanceItems { get; set; } = new List<ClearanceItem>();
}
