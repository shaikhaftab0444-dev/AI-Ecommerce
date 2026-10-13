using BrandsStore.Data;
using BrandsStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrandsStore.Controllers
{
    [Authorize(Roles = "User")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // ================================================
        // VIEW CART
        // ================================================
        public IActionResult Index()
        {
            try
            {
                var userId = GetUserId();
                var cartItems = _context.CartItems
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Where(c => c.UserId == userId)
                    .ToList();

                ViewBag.Total = cartItems.Sum(c => c.Product.Price * c.Quantity);
                ViewBag.ItemCount = cartItems.Sum(c => c.Quantity);

                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["Error"] = "Error loading cart";
                return View(new List<CartItem>());
            }
        }

        // ================================================
        // ADD TO CART
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = GetUserId();
                var product = await _context.Products.FindAsync(productId);

                if (product == null || !product.IsActive)
                {
                    TempData["Error"] = "Product not found or unavailable";
                    return RedirectToAction("Products", "Home");
                }

                if (product.StockQuantity < quantity)
                {
                    TempData["Error"] = $"Only {product.StockQuantity} units available";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.StockQuantity)
                    {
                        TempData["Error"] = $"Cannot add more. Only {product.StockQuantity} units available";
                        return RedirectToAction("Index");
                    }
                    existingItem.Quantity += quantity;
                    _context.CartItems.Update(existingItem);
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        AddedDate = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"{product.Name} added to cart!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {productId} to cart");
                TempData["Error"] = "Error adding product to cart";
                return RedirectToAction("Products", "Home");
            }
        }

        // ================================================
        // BUY NOW — Adds product to cart then goes to Checkout
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            try
            {
                var userId = GetUserId();
                var product = await _context.Products.FindAsync(productId);

                if (product == null || !product.IsActive)
                {
                    TempData["Error"] = "Product not found or unavailable";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                if (quantity < 1) quantity = 1;

                if (product.StockQuantity < quantity)
                {
                    TempData["Error"] = $"Only {product.StockQuantity} units available";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                // Add or update in cart
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (existingItem != null)
                {
                    // Set quantity to the requested amount (Buy Now behavior)
                    existingItem.Quantity = quantity;
                    _context.CartItems.Update(existingItem);
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        AddedDate = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"BuyNow: Product {productId} added for User {userId}, redirecting to Checkout");

                // Redirect directly to Checkout (GET) — this is a redirect so it becomes GET
                return RedirectToAction("Checkout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in BuyNow for product {productId}");
                TempData["Error"] = "Error processing request";
                return RedirectToAction("ProductDetails", "Home", new { id = productId });
            }
        }

        // ================================================
        // UPDATE QUANTITY
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                if (quantity < 1)
                {
                    TempData["Error"] = "Quantity must be at least 1";
                    return RedirectToAction("Index");
                }

                var userId = GetUserId();
                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    TempData["Error"] = "Cart item not found";
                    return RedirectToAction("Index");
                }

                if (quantity > cartItem.Product.StockQuantity)
                {
                    TempData["Error"] = $"Only {cartItem.Product.StockQuantity} units available";
                    return RedirectToAction("Index");
                }

                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Quantity updated";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating cart item {cartItemId}");
                TempData["Error"] = "Error updating quantity";
                return RedirectToAction("Index");
            }
        }

        // ================================================
        // REMOVE FROM CART
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var userId = GetUserId();
                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

                if (cartItem != null)
                {
                    var productName = cartItem.Product.Name;
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"{productName} removed from cart";
                }
                else
                {
                    TempData["Error"] = "Cart item not found";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cart item {cartItemId}");
                TempData["Error"] = "Error removing item";
                return RedirectToAction("Index");
            }
        }

        // ================================================
        // CLEAR CART
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetUserId();
                var cartItems = _context.CartItems.Where(c => c.UserId == userId);
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cart cleared";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Error clearing cart";
                return RedirectToAction("Index");
            }
        }

        // ================================================
        // CHECKOUT (GET)
        // ================================================
        [HttpGet]
        public IActionResult Checkout()
        {
            try
            {
                var userId = GetUserId();
                var cartItems = _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToList();

                if (!cartItems.Any())
                {
                    TempData["Warning"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                foreach (var item in cartItems)
                {
                    if (item.Quantity > item.Product.StockQuantity)
                    {
                        TempData["Error"] = $"{item.Product.Name} has only {item.Product.StockQuantity} units available";
                        return RedirectToAction("Index");
                    }
                }

                ViewBag.CartItems = cartItems;
                ViewBag.Total = cartItems.Sum(c => c.Product.Price * c.Quantity);
                ViewBag.ItemCount = cartItems.Sum(c => c.Quantity);

                var user = _context.Users.Find(userId);
                ViewBag.UserName = user?.FullName;
                ViewBag.UserEmail = user?.Email;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout");
                TempData["Error"] = "Error loading checkout";
                return RedirectToAction("Index");
            }
        }

        // ================================================
        // PLACE ORDER (POST)
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(shippingAddress))
                {
                    TempData["Error"] = "Shipping address is required";
                    return RedirectToAction("Checkout");
                }

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    TempData["Error"] = "Phone number is required";
                    return RedirectToAction("Checkout");
                }

                var userId = GetUserId();
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                decimal totalAmount = 0;
                foreach (var item in cartItems)
                {
                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {item.Product.Name}";
                        return RedirectToAction("Checkout");
                    }
                    totalAmount += item.Product.Price * item.Quantity;
                }

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    ShippingAddress = shippingAddress.Trim(),
                    PhoneNumber = phoneNumber.Trim()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price
                    });
                    item.Product.StockQuantity -= item.Quantity;
                    _context.Products.Update(item.Product);
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {order.OrderId} placed by User {userId}");
                TempData["Success"] = $"Order placed successfully! Order ID: #{order.OrderId}";
                return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["Error"] = $"Error placing order: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }

        // ================================================
        // ORDER CONFIRMATION
        // ================================================
        public IActionResult OrderConfirmation(int orderId)
        {
            try
            {
                var userId = GetUserId();
                var order = _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.Category)
                    .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction("Index", "Home");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order confirmation for Order {orderId}");
                TempData["Error"] = "Error loading order details";
                return RedirectToAction("Index", "Home");
            }
        }

        // ================================================
        // MY ORDERS
        // ================================================
        public IActionResult MyOrders()
        {
            try
            {
                var userId = GetUserId();
                var orders = _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "Error loading orders";
                return View(new List<Order>());
            }
        }

        // ================================================
        // ORDER DETAILS
        // ================================================
        public IActionResult OrderDetails(int id)
        {
            try
            {
                var userId = GetUserId();
                var order = _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.Category)
                    .FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction("MyOrders");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order details for Order {id}");
                TempData["Error"] = "Error loading order details";
                return RedirectToAction("MyOrders");
            }
        }
        // ================================================
        // CANCEL ORDER
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string cancelReason, string cancelReasonOther)
        {
            try
            {
                var userId = GetUserId();
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction("MyOrders");
                }

                if (order.Status != "Pending" && order.Status != "Processing")
                {
                    TempData["Error"] = "This order can no longer be cancelled.";
                    return RedirectToAction("OrderDetails", new { id = orderId });
                }

                var reason = cancelReason == "Other"
                    ? (string.IsNullOrWhiteSpace(cancelReasonOther) ? "Other" : cancelReasonOther)
                    : cancelReason;

                order.Status = "Cancelled";

                // Restore stock for each item
                var orderDetails = _context.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .Include(od => od.Product)
                    .ToList();

                foreach (var item in orderDetails)
                {
                    if (item.Product != null)
                        item.Product.StockQuantity += item.Quantity;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order #{orderId:D4} has been cancelled. Reason: {reason}";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order {orderId}");
                TempData["Error"] = "Could not cancel order. Please contact support.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
        }

        // ================================================
        // GET CART COUNT (API)
        // ================================================
        [HttpGet]
        public IActionResult GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated) return Json(0);
                var userId = GetUserId();
                var cartCount = _context.CartItems
                    .Where(c => c.UserId == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;
                return Json(cartCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(0);
            }
        }
    }
}