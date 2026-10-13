using BrandsStore.Models;

namespace BrandsStore.ViewModel
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<Size> AvailableSizes { get; set; }
        public List<Color> AvailableColors { get; set; }

        // Selected variant properties
        public int? SelectedSizeId { get; set; }
        public int? SelectedColorId { get; set; }
        public int SelectedQuantity { get; set; } = 1;
    }
}
