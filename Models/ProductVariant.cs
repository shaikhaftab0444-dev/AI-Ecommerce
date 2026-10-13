// ================================================
// ProductVariant Model (for managing stock by size/color)
// ================================================
using BrandsStore.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }

    [Required(ErrorMessage = "Product is required")]
    public int ProductId { get; set; }

    [Display(Name = "Size")]
    public int? SizeId { get; set; }

    [Display(Name = "Color")]
    public int? ColorId { get; set; }

    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
    [Display(Name = "Stock Quantity")]
    public int StockQuantity { get; set; } = 0;

    [Display(Name = "Price Override")]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }

    [StringLength(100)]
    [Display(Name = "SKU")]
    public string? SKU { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("SizeId")]
    public virtual Size? Size { get; set; }

    [ForeignKey("ColorId")]
    public virtual Color? Color { get; set; }
}
