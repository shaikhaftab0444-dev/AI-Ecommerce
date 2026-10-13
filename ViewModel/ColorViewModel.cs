// ================================================
// Color Management ViewModel
// ================================================
using System.ComponentModel.DataAnnotations;

public class ColorViewModel
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(50)]
    public string ColorName { get; set; }

    [Required]
    [StringLength(20)]
    public string ColorCode { get; set; }

    public bool IsActive { get; set; } = true;
}