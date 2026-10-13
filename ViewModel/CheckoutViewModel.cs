using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace BrandsStore.ViewModel
{
    public class CheckoutViewModel
    {

        [Required(ErrorMessage = "Shipping address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Order Notes")]
        [StringLength(500)]
        public string OrderNotes { get; set; }

        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";
    }
}
