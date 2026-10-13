// ================================================
// Size Management ViewModel
// ================================================
using System.ComponentModel.DataAnnotations;

public class SizeViewModel
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(50)]
    public string SizeName { get; set; }

    [Required]
    public int SizeOrder { get; set; }

    public bool IsActive { get; set; } = true;
}