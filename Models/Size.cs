using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandsStore.Models
{
    // ================================================
    // Size Model
    // ================================================
    public class Size
    {
        [Key]
        public int SizeId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Size name is required")]
        [StringLength(50)]
        [Display(Name = "Size Name")]
        public string SizeName { get; set; }

        [Required(ErrorMessage = "Size order is required")]
        [Display(Name = "Display Order")]
        public int SizeOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<ProductVariant> ProductVariants { get; set; }
    }
}