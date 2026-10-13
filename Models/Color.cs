using BrandsStore.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// ================================================
// Color Model
// ================================================
public class Color
{
    [Key]
    public int ColorId { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Color name is required")]
    [StringLength(50)]
    [Display(Name = "Color Name")]
    public string ColorName { get; set; }

    [Required(ErrorMessage = "Color code is required")]
    [StringLength(20)]
    [Display(Name = "Color Code")]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
        ErrorMessage = "Please enter a valid hex color code (e.g., #FF0000)")]
    public string ColorCode { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; }

    public virtual ICollection<ProductVariant> ProductVariants { get; set; }
}
