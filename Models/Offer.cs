using System;
using System.ComponentModel.DataAnnotations;

namespace BrandsStore.Models
{
    public class Offer
    {
        public int OfferId { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Subtitle { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        // e.g. "50% OFF", "FLAT ₹200"
        [MaxLength(40)]
        public string? BadgeText { get; set; }

        // e.g. "WINTER SALE 2026"
        [MaxLength(60)]
        public string? EyebrowText { get; set; }

        // Button 1
        [MaxLength(60)]
        public string? PrimaryButtonText { get; set; }
        [MaxLength(200)]
        public string? PrimaryButtonUrl { get; set; }

        // Button 2
        [MaxLength(60)]
        public string? SecondaryButtonText { get; set; }
        [MaxLength(200)]
        public string? SecondaryButtonUrl { get; set; }

        // Hero image displayed on the right side
        [MaxLength(400)]
        public string? ImageUrl { get; set; }

        // Optional full-bleed background image for the slide
        [MaxLength(400)]
        public string? BackgroundImageUrl { get; set; }

        // Accent color for this slide e.g. "#e8365d"
        [MaxLength(20)]
        public string AccentColor { get; set; } = "#e8365d";

        // Pill 1 (floating badge top-right)
        [MaxLength(60)]
        public string? Pill1Title { get; set; }
        [MaxLength(60)]
        public string? Pill1Subtitle { get; set; }

        // Pill 2 (floating badge bottom-left)
        [MaxLength(60)]
        public string? Pill2Title { get; set; }
        [MaxLength(60)]
        public string? Pill2Subtitle { get; set; }

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}