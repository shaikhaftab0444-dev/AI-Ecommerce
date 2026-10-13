using System.ComponentModel.DataAnnotations;

namespace BrandsStore.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Product> Products { get; set; }
        // Add these navigation properties to your existing Category model
        public virtual ICollection<Size> Sizes { get; set; }
        public virtual ICollection<Color> Colors { get; set; }
    }
}
