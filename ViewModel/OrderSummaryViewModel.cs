using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace BrandsStore.ViewModel
{
    public class OrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public int TotalItems { get; set; }
        public string ShippingAddress { get; set; }
        public string PhoneNumber { get; set; }
    }
}
