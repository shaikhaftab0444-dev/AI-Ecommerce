using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace BrandsStore.ViewModel
{
    public class OrderFilterViewModel
    {
        [Display(Name = "Order Status")]
        public string Status { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Customer Email")]
        public string CustomerEmail { get; set; }
    }
}
