using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;


namespace BrandsStore.ViewModel
{
    public class ProductSearchViewModel
    {
        [Display(Name = "Search")]
        public string SearchTerm { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Min Price")]
        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Max Price")]
        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Sort By")]
        public string SortBy { get; set; } // "name", "price-low", "price-high", "newest"

        [Display(Name = "In Stock Only")]
        public bool InStockOnly { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
