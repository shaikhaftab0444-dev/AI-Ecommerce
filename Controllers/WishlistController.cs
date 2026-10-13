using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BrandsStore.Data;
using BrandsStore.Models;

namespace BrandsStore.Controllers
{
    [Authorize(Roles = "User")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to get current user ID
        private int GetCurrentUserId()
        {
            var email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user?.UserId ?? 0;
        }

        // GET: Wishlist
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var wishlistItems = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .ThenInclude(p => p.Category)
                .Select(w => w.Product)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: Wishlist/AddToWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();

                if (userId == 0)
                {
                    TempData["Error"] = "Please login to add items to wishlist!";
                    return RedirectToAction("Login", "Account");
                }

                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found!";
                    return RedirectToAction("Products", "Home");
                }

                // Check if item already exists in wishlist
                var existingItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existingItem != null)
                {
                    TempData["Error"] = "This item is already in your wishlist!";
                    return RedirectToAction("Index");
                }

                // Add to wishlist
                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedDate = DateTime.Now
                };

                _context.Wishlists.Add(wishlistItem);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{product.Name} has been added to your wishlist! ❤️";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while adding to wishlist.";
                return RedirectToAction("Products", "Home");
            }
        }

        // POST: Wishlist/RemoveFromWishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();

                var wishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem != null)
                {
                    _context.Wishlists.Remove(wishlistItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Item removed from your wishlist!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while removing from wishlist.";
                return RedirectToAction("Index");
            }
        }

        // GET: Wishlist/ClearWishlist
        public async Task<IActionResult> ClearWishlist()
        {
            try
            {
                var userId = GetCurrentUserId();

                var wishlistItems = await _context.Wishlists
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                if (wishlistItems.Any())
                {
                    _context.Wishlists.RemoveRange(wishlistItems);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Wishlist cleared successfully!";
                }
                else
                {
                    TempData["Error"] = "Your wishlist is already empty!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while clearing wishlist.";
                return RedirectToAction("Index");
            }
        }

        // POST: Wishlist/AddAllToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllToCart()
        {
            try
            {
                var userId = GetCurrentUserId();

                var wishlistItems = await _context.Wishlists
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Product)
                    .ToListAsync();

                if (!wishlistItems.Any())
                {
                    TempData["Error"] = "Your wishlist is empty!";
                    return RedirectToAction("Index");
                }

                int addedCount = 0;
                foreach (var wishlistItem in wishlistItems)
                {
                    if (wishlistItem.Product.StockQuantity > 0)
                    {
                        // Check if item already exists in cart
                        var cartItem = await _context.CartItems
                            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == wishlistItem.ProductId);

                        if (cartItem != null)
                        {
                            // Update quantity
                            cartItem.Quantity += 1;
                        }
                        else
                        {
                            // Add new cart item
                            var newCartItem = new CartItem
                            {
                                UserId = userId,
                                ProductId = wishlistItem.ProductId,
                                Quantity = 1,
                                AddedDate = DateTime.Now
                            };
                            _context.CartItems.Add(newCartItem);
                        }
                        addedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                if (addedCount > 0)
                {
                    TempData["Success"] = $"{addedCount} item(s) added to cart successfully! 🛒";
                    return RedirectToAction("Index", "Cart");
                }
                else
                {
                    TempData["Error"] = "No items were added (all items are out of stock)!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while adding items to cart.";
                return RedirectToAction("Index");
            }
        }

        // GET: Wishlist/GetWishlistCount (for navbar badge)
        [HttpGet]
        public async Task<IActionResult> GetWishlistCount()
        {
            var userId = GetCurrentUserId();
            var count = await _context.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count = count });
        }
    }
}