// ================================================
// Product Variant Item (for form submission)
// ================================================
using System.ComponentModel.DataAnnotations;

namespace BrandsStore.ViewModel
{
    public class ProductVariantItem
    {
        public int VariantId { get; set; }
        public int? SizeId { get; set; }
        public int? ColorId { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater")]
        public int StockQuantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [StringLength(100)]
        public string? SKU { get; set; }

        public bool IsActive { get; set; } = true;

        // For mapping from frontend
        public int? SizeIndex { get; set; }
        public int? ColorIndex { get; set; }
    }
}