using BrandsStore.Models;
using Microsoft.EntityFrameworkCore;

namespace BrandsStore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

        // Product Variant DbSets
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        // ✅ NEW: Offers (hero slides on home page)
        public DbSet<Offer> Offers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================================================
            // PRODUCT CONFIGURATION
            // ================================================
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================================================
            // ORDER CONFIGURATION
            // ================================================
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================================================
            // ORDER DETAIL CONFIGURATION
            // ================================================
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // ================================================
            // WISHLIST CONFIGURATION
            // ================================================
            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();

            // ================================================
            // SIZE CONFIGURATION
            // ================================================
            modelBuilder.Entity<Size>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Sizes)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Size>()
                .HasIndex(s => s.CategoryId)
                .HasDatabaseName("IX_Sizes_CategoryId");

            // ================================================
            // COLOR CONFIGURATION
            // ================================================
            modelBuilder.Entity<Color>()
                .HasOne(c => c.Category)
                .WithMany(cat => cat.Colors)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Color>()
                .HasIndex(c => c.CategoryId)
                .HasDatabaseName("IX_Colors_CategoryId");

            // ================================================
            // PRODUCT VARIANT CONFIGURATION
            // ================================================
            modelBuilder.Entity<ProductVariant>()
                .Property(pv => pv.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Size)
                .WithMany(s => s.ProductVariants)
                .HasForeignKey(pv => pv.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Color)
                .WithMany(c => c.ProductVariants)
                .HasForeignKey(pv => pv.ColorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.ProductId)
                .HasDatabaseName("IX_ProductVariants_ProductId");

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.SizeId)
                .HasDatabaseName("IX_ProductVariants_SizeId");

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.ColorId)
                .HasDatabaseName("IX_ProductVariants_ColorId");

            // ================================================
            // OFFER CONFIGURATION
            // ================================================
            modelBuilder.Entity<Offer>()
                .Property(o => o.AccentColor)
                .HasDefaultValue("#e8365d");

            // ================================================
            // SEED DATA - ADMIN USER
            // ================================================
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "Admin User",
                    Email = "admin@brandsstore.com",
                    Password = "Admin@123",
                    Role = "Admin",
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    PasswordResetOtpHash = null,
                    OtpExpiryTime = null,
                    OtpAttemptCount = 0,
                    OtpLockoutEnd = null,
                    PasswordResetToken = null,
                    ResetTokenExpiryTime = null
                }
            );

            // ================================================
            // SEED DATA - CATEGORIES
            // ================================================
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Electronics", Description = "Electronic devices, gadgets, laptops, phones, and accessories", IsActive = true },
                new Category { CategoryId = 2, Name = "Clothing", Description = "Fashion apparel, casual wear, and clothing items", IsActive = true },
                new Category { CategoryId = 3, Name = "Books", Description = "Books, novels, textbooks, and publications", IsActive = true },
                new Category { CategoryId = 4, Name = "Home & Kitchen", Description = "Home appliances, kitchen items, and household goods", IsActive = true },
                new Category { CategoryId = 5, Name = "Shoes", Description = "Footwear including sneakers, formal shoes, boots, and sandals", IsActive = true },
                new Category { CategoryId = 6, Name = "Shirts", Description = "Casual shirts, formal shirts, t-shirts, and tops", IsActive = true },
                new Category { CategoryId = 7, Name = "Pants", Description = "Jeans, trousers, casual pants, and formal wear", IsActive = true },
                new Category { CategoryId = 8, Name = "Accessories", Description = "Watches, belts, bags, wallets, and fashion accessories", IsActive = true },
                new Category { CategoryId = 9, Name = "Sports & Fitness", Description = "Sports equipment, gym wear, and fitness accessories", IsActive = true },
                new Category { CategoryId = 10, Name = "Jewelry", Description = "Rings, necklaces, bracelets, and fashion jewelry", IsActive = true }
            );

            // ================================================
            // SEED DATA - SIZES
            // ================================================
            // Clothing (Cat 2)
            modelBuilder.Entity<Size>().HasData(
                new Size { SizeId = 1, CategoryId = 2, SizeName = "XS", SizeOrder = 1, IsActive = true },
                new Size { SizeId = 2, CategoryId = 2, SizeName = "S", SizeOrder = 2, IsActive = true },
                new Size { SizeId = 3, CategoryId = 2, SizeName = "M", SizeOrder = 3, IsActive = true },
                new Size { SizeId = 4, CategoryId = 2, SizeName = "L", SizeOrder = 4, IsActive = true },
                new Size { SizeId = 5, CategoryId = 2, SizeName = "XL", SizeOrder = 5, IsActive = true },
                new Size { SizeId = 6, CategoryId = 2, SizeName = "XXL", SizeOrder = 6, IsActive = true }
            );
            // Shoes (Cat 5)
            modelBuilder.Entity<Size>().HasData(
                new Size { SizeId = 7, CategoryId = 5, SizeName = "7", SizeOrder = 1, IsActive = true },
                new Size { SizeId = 8, CategoryId = 5, SizeName = "8", SizeOrder = 2, IsActive = true },
                new Size { SizeId = 9, CategoryId = 5, SizeName = "9", SizeOrder = 3, IsActive = true },
                new Size { SizeId = 10, CategoryId = 5, SizeName = "10", SizeOrder = 4, IsActive = true },
                new Size { SizeId = 11, CategoryId = 5, SizeName = "11", SizeOrder = 5, IsActive = true }
            );
            // Shirts (Cat 6)
            modelBuilder.Entity<Size>().HasData(
                new Size { SizeId = 12, CategoryId = 6, SizeName = "S", SizeOrder = 1, IsActive = true },
                new Size { SizeId = 13, CategoryId = 6, SizeName = "M", SizeOrder = 2, IsActive = true },
                new Size { SizeId = 14, CategoryId = 6, SizeName = "L", SizeOrder = 3, IsActive = true },
                new Size { SizeId = 15, CategoryId = 6, SizeName = "XL", SizeOrder = 4, IsActive = true }
            );

            // ================================================
            // SEED DATA - COLORS
            // ================================================
            // Electronics (Cat 1)
            modelBuilder.Entity<Color>().HasData(
                new Color { ColorId = 1, CategoryId = 1, ColorName = "Black", ColorCode = "#000000", IsActive = true },
                new Color { ColorId = 2, CategoryId = 1, ColorName = "White", ColorCode = "#FFFFFF", IsActive = true },
                new Color { ColorId = 3, CategoryId = 1, ColorName = "Silver", ColorCode = "#C0C0C0", IsActive = true }
            );
            // Clothing (Cat 2)
            modelBuilder.Entity<Color>().HasData(
                new Color { ColorId = 4, CategoryId = 2, ColorName = "Black", ColorCode = "#000000", IsActive = true },
                new Color { ColorId = 5, CategoryId = 2, ColorName = "White", ColorCode = "#FFFFFF", IsActive = true },
                new Color { ColorId = 6, CategoryId = 2, ColorName = "Red", ColorCode = "#DC2626", IsActive = true },
                new Color { ColorId = 7, CategoryId = 2, ColorName = "Blue", ColorCode = "#3B82F6", IsActive = true },
                new Color { ColorId = 8, CategoryId = 2, ColorName = "Green", ColorCode = "#10B981", IsActive = true }
            );
            // Shoes (Cat 5)
            modelBuilder.Entity<Color>().HasData(
                new Color { ColorId = 9, CategoryId = 5, ColorName = "Black", ColorCode = "#000000", IsActive = true },
                new Color { ColorId = 10, CategoryId = 5, ColorName = "White", ColorCode = "#FFFFFF", IsActive = true },
                new Color { ColorId = 11, CategoryId = 5, ColorName = "Brown", ColorCode = "#92400E", IsActive = true }
            );
            // Shirts (Cat 6)
            modelBuilder.Entity<Color>().HasData(
                new Color { ColorId = 12, CategoryId = 6, ColorName = "White", ColorCode = "#FFFFFF", IsActive = true },
                new Color { ColorId = 13, CategoryId = 6, ColorName = "Black", ColorCode = "#000000", IsActive = true },
                new Color { ColorId = 14, CategoryId = 6, ColorName = "Blue", ColorCode = "#3B82F6", IsActive = true },
                new Color { ColorId = 15, CategoryId = 6, ColorName = "Gray", ColorCode = "#6B7280", IsActive = true }
            );
        }
    }
}