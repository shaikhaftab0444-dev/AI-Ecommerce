using System.ComponentModel.DataAnnotations;

namespace BrandsStore.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // "Admin" or "User"

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // NEW: Add these properties for password reset
        public string? PasswordResetOtpHash { get; set; }
        public DateTime? OtpExpiryTime { get; set; }
        public int OtpAttemptCount { get; set; }
        public DateTime? OtpLockoutEnd { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiryTime { get; set; }

        // Navigation property
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
    }
}
