using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BrandsStore.ViewModel
{
    public class ProductVariantViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; }

        // Make SKU optional - we'll auto-generate if not provided
        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        [Display(Name = "SKU (Optional - Auto-generated if empty)")]
        public string? SKU { get; set; }

        [Display(Name = "Description")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Base Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
        [Display(Name = "Total Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Product Images")]
        public List<IFormFile>? ImageFiles { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Custom Sizes and Colors
        public List<string>? CustomSizes { get; set; }
        public List<CustomColorDto>? CustomColors { get; set; }

        // Variants
        public List<ProductVariantItem>? Variants { get; set; }
    }
}