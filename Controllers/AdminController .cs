using BrandsStore.Data;
using BrandsStore.Helpers;
using BrandsStore.Models;
using BrandsStore.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrandsStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AdminController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // ================================================
        // DASHBOARD
        // ================================================
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentYear = DateTime.Now.Year;

                ViewBag.TotalProducts = await _context.Products.CountAsync();
                ViewBag.TotalCategories = await _context.Categories.CountAsync();
                ViewBag.TotalOrders = await _context.Orders.CountAsync();
                ViewBag.TotalUsers = await _context.Users.Where(u => u.Role == "User").CountAsync();
                ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                var monthlyRevenue = new decimal[12];
                var revenueByMonth = await _context.Orders
                    .Where(o => o.OrderDate.Year == currentYear)
                    .GroupBy(o => o.OrderDate.Month)
                    .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount) })
                    .ToListAsync();
                foreach (var item in revenueByMonth)
                    monthlyRevenue[item.Month - 1] = item.Total;
                ViewBag.MonthlyRevenue = monthlyRevenue;

                var monthlyOrders = new int[12];
                var ordersByMonth = await _context.Orders
                    .Where(o => o.OrderDate.Year == currentYear)
                    .GroupBy(o => o.OrderDate.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();
                foreach (var item in ordersByMonth)
                    monthlyOrders[item.Month - 1] = item.Count;
                ViewBag.MonthlyOrders = monthlyOrders;

                var rawCategoryStats = await _context.Products
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.Category.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(8)
                    .ToListAsync();
                ViewBag.CategoryStats = rawCategoryStats
                    .Select(c => new { c.Name, c.Count })
                    .ToList();

                ViewBag.RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(8)
                    .ToListAsync();

                // ✅ Offers for dashboard panel
                ViewBag.RecentOffers = await _context.Offers
                    .OrderBy(o => o.SortOrder)
                    .ThenByDescending(o => o.CreatedAt)
                    .ToListAsync();
                ViewBag.TotalOffers = await _context.Offers.CountAsync();
                ViewBag.ActiveOffers = await _context.Offers.CountAsync(o => o.IsActive);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Error loading dashboard data";
                ViewBag.TotalProducts = 0;
                ViewBag.TotalCategories = 0;
                ViewBag.TotalOrders = 0;
                ViewBag.TotalUsers = 0;
                ViewBag.TotalRevenue = 0m;
                ViewBag.MonthlyRevenue = new decimal[12];
                ViewBag.MonthlyOrders = new int[12];
                ViewBag.CategoryStats = new[] { new { Name = "No Data", Count = 1 } };
                ViewBag.RecentOrders = null;
                ViewBag.RecentOffers = new List<Offer>();
                ViewBag.TotalOffers = 0;
                ViewBag.ActiveOffers = 0;
                return View();
            }
        }

        // ================================================
        // OFFERS — LIST
        // ================================================
        public async Task<IActionResult> Offers()
        {
            try
            {
                var offers = await _context.Offers
                    .OrderBy(o => o.SortOrder)
                    .ThenByDescending(o => o.CreatedAt)
                    .ToListAsync();
                return View(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading offers");
                TempData["Error"] = "Error loading offers.";
                return View(new List<Offer>());
            }
        }

        // ================================================
        // OFFERS — CREATE
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOffer(Offer offer, IFormFile? HeroImageFile, IFormFile? BgImageFile)
        {
            try
            {
                offer.IsActive = Request.Form["IsActive"].Contains("true");
                offer.CreatedAt = DateTime.Now;

                if (HeroImageFile != null && HeroImageFile.Length > 0)
                    offer.ImageUrl = await FileUploadHelper.UploadImage(HeroImageFile, _webHostEnvironment, "offers");

                if (BgImageFile != null && BgImageFile.Length > 0)
                    offer.BackgroundImageUrl = await FileUploadHelper.UploadImage(BgImageFile, _webHostEnvironment, "offers");

                _context.Offers.Add(offer);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Offer '{offer.Title}' created successfully!";
                return RedirectToAction("Offers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating offer");
                TempData["Error"] = "Error creating offer. Please try again.";
                return RedirectToAction("Offers");
            }
        }

        // ================================================
        // OFFERS — EDIT
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOffer(Offer offer, IFormFile? HeroImageFile, IFormFile? BgImageFile)
        {
            try
            {
                var existing = await _context.Offers.FindAsync(offer.OfferId);
                if (existing == null)
                {
                    TempData["Error"] = "Offer not found.";
                    return RedirectToAction("Offers");
                }

                existing.Title = offer.Title;
                existing.Subtitle = offer.Subtitle;
                existing.Description = offer.Description;
                existing.EyebrowText = offer.EyebrowText;
                existing.BadgeText = offer.BadgeText;
                existing.PrimaryButtonText = offer.PrimaryButtonText;
                existing.PrimaryButtonUrl = offer.PrimaryButtonUrl;
                existing.SecondaryButtonText = offer.SecondaryButtonText;
                existing.SecondaryButtonUrl = offer.SecondaryButtonUrl;
                existing.AccentColor = offer.AccentColor;
                existing.Pill1Title = offer.Pill1Title;
                existing.Pill1Subtitle = offer.Pill1Subtitle;
                existing.Pill2Title = offer.Pill2Title;
                existing.Pill2Subtitle = offer.Pill2Subtitle;
                existing.SortOrder = offer.SortOrder;
                existing.IsActive = Request.Form["IsActive"].Contains("true");
                existing.UpdatedAt = DateTime.Now;

                // Hero image: new upload replaces old; hidden field keeps existing if no new upload
                if (HeroImageFile != null && HeroImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.ImageUrl))
                        FileUploadHelper.DeleteImage(existing.ImageUrl, _webHostEnvironment);
                    existing.ImageUrl = await FileUploadHelper.UploadImage(HeroImageFile, _webHostEnvironment, "offers");
                }
                else if (offer.ImageUrl != null)
                {
                    existing.ImageUrl = offer.ImageUrl; // keep or clear based on hidden field
                }

                // Background image
                if (BgImageFile != null && BgImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.BackgroundImageUrl))
                        FileUploadHelper.DeleteImage(existing.BackgroundImageUrl, _webHostEnvironment);
                    existing.BackgroundImageUrl = await FileUploadHelper.UploadImage(BgImageFile, _webHostEnvironment, "offers");
                }
                else if (offer.BackgroundImageUrl != null)
                {
                    existing.BackgroundImageUrl = offer.BackgroundImageUrl;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Offer '{offer.Title}' updated!";
                return RedirectToAction("Offers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing offer");
                TempData["Error"] = "Error updating offer.";
                return RedirectToAction("Offers");
            }
        }

        // ================================================
        // OFFERS — TOGGLE ACTIVE
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOffer(int id)
        {
            try
            {
                var offer = await _context.Offers.FindAsync(id);
                if (offer == null)
                {
                    TempData["Error"] = "Offer not found.";
                    return RedirectToAction("Offers");
                }
                offer.IsActive = !offer.IsActive;
                offer.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Offer '{offer.Title}' is now {(offer.IsActive ? "active" : "inactive")}.";
                return RedirectToAction("Offers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling offer");
                TempData["Error"] = "Error updating offer status.";
                return RedirectToAction("Offers");
            }
        }

        // ================================================
        // OFFERS — DELETE
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            try
            {
                var offer = await _context.Offers.FindAsync(id);
                if (offer != null)
                {
                    _context.Offers.Remove(offer);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Offer deleted.";
                }
                return RedirectToAction("Offers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting offer");
                TempData["Error"] = "Error deleting offer.";
                return RedirectToAction("Offers");
            }
        }

        // ================================================
        // PRODUCTS MANAGEMENT
        // ================================================
        public async Task<IActionResult> Products()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["Error"] = "Error loading products";
                return View(new List<Product>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            try
            {
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View(new ProductVariantViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product page");
                TempData["Error"] = "Error loading page";
                return RedirectToAction("Products");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductVariantViewModel model)
        {
            try
            {
                ModelState.Remove("ImageFile");
                ModelState.Remove("ImageUrl");
                ModelState.Remove("ImageFiles");
                ModelState.Remove("SKU");

                _logger.LogInformation($"=== CREATE PRODUCT START ===");
                _logger.LogInformation($"Name: {model.Name}, Category: {model.CategoryId}, Price: {model.Price}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning($"Model validation failed: {string.Join(", ", errors)}");
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                    TempData["Error"] = "Please fill all required fields: " + string.Join(", ", errors);
                    return View(model);
                }

                string productSKU = string.IsNullOrWhiteSpace(model.SKU)
                    ? $"PROD-{DateTime.Now:yyyyMMddHHmmss}"
                    : model.SKU.Trim();

                string? imageUrl = null;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    try
                    {
                        imageUrl = await FileUploadHelper.UploadImage(model.ImageFile, _webHostEnvironment, "products");
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogError(imgEx, "Error uploading image");
                        TempData["Warning"] = $"Image upload failed: {imgEx.Message}";
                    }
                }

                var product = new Product
                {
                    Name = model.Name?.Trim(),
                    SKU = productSKU,
                    CategoryId = model.CategoryId,
                    Description = model.Description?.Trim(),
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    ImageUrl = imageUrl,
                    IsActive = model.IsActive,
                    CreatedDate = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var sizeMapping = new Dictionary<int, int>();
                var colorMapping = new Dictionary<int, int>();

                if (model.CustomSizes != null && model.CustomSizes.Any())
                {
                    for (int i = 0; i < model.CustomSizes.Count; i++)
                    {
                        var sizeName = model.CustomSizes[i]?.Trim();
                        if (string.IsNullOrWhiteSpace(sizeName)) continue;

                        var existing = await _context.Sizes.FirstOrDefaultAsync(
                            s => s.SizeName.ToLower() == sizeName.ToLower() && s.CategoryId == model.CategoryId);

                        if (existing != null)
                        {
                            sizeMapping[i] = existing.SizeId;
                        }
                        else
                        {
                            var ns = new Size { CategoryId = model.CategoryId, SizeName = sizeName, SizeOrder = i + 1, IsActive = true };
                            _context.Sizes.Add(ns);
                            await _context.SaveChangesAsync();
                            sizeMapping[i] = ns.SizeId;
                        }
                    }
                }

                if (model.CustomColors != null && model.CustomColors.Any())
                {
                    for (int i = 0; i < model.CustomColors.Count; i++)
                    {
                        var color = model.CustomColors[i];
                        if (string.IsNullOrWhiteSpace(color?.Name)) continue;

                        var existing = await _context.Colors.FirstOrDefaultAsync(
                            c => c.ColorName.ToLower() == color.Name.ToLower() && c.CategoryId == model.CategoryId);

                        if (existing != null)
                        {
                            colorMapping[i] = existing.ColorId;
                        }
                        else
                        {
                            var nc = new Color { CategoryId = model.CategoryId, ColorName = color.Name.Trim(), ColorCode = color.Code, IsActive = true };
                            _context.Colors.Add(nc);
                            await _context.SaveChangesAsync();
                            colorMapping[i] = nc.ColorId;
                        }
                    }
                }

                if (model.Variants != null && model.Variants.Any())
                {
                    foreach (var variantItem in model.Variants)
                    {
                        int? actualSizeId = variantItem.SizeIndex.HasValue && sizeMapping.ContainsKey(variantItem.SizeIndex.Value) ? sizeMapping[variantItem.SizeIndex.Value] : null;
                        int? actualColorId = variantItem.ColorIndex.HasValue && colorMapping.ContainsKey(variantItem.ColorIndex.Value) ? colorMapping[variantItem.ColorIndex.Value] : null;

                        string? variantSKU = variantItem.SKU;
                        if (string.IsNullOrWhiteSpace(variantSKU))
                        {
                            var sp = actualSizeId.HasValue ? (await _context.Sizes.FindAsync(actualSizeId.Value))?.SizeName ?? "NA" : "NA";
                            var cp = actualColorId.HasValue ? (await _context.Colors.FindAsync(actualColorId.Value))?.ColorName ?? "NA" : "NA";
                            variantSKU = $"{productSKU}-{sp}-{cp}";
                        }

                        _context.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = product.ProductId,
                            SizeId = actualSizeId,
                            ColorId = actualColorId,
                            StockQuantity = variantItem.StockQuantity,
                            Price = variantItem.Price,
                            SKU = variantSKU,
                            IsActive = variantItem.IsActive,
                            CreatedAt = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Product '{product.Name}' created successfully with SKU: {product.SKU}!";
                return RedirectToAction("Products");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error creating product");
                TempData["Error"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating product");
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    TempData["Error"] = "Product not found";
                    return RedirectToAction("Products");
                }

                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == id)
                    .Select(v => new ProductVariantItem
                    {
                        VariantId = v.VariantId,
                        SizeId = v.SizeId,
                        ColorId = v.ColorId,
                        StockQuantity = v.StockQuantity,
                        Price = v.Price,
                        SKU = v.SKU,
                        IsActive = v.IsActive
                    })
                    .ToListAsync();

                var model = new ProductVariantViewModel
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    CategoryId = product.CategoryId,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    Variants = variants
                };

                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading product {id} for editing");
                TempData["Error"] = "Error loading product";
                return RedirectToAction("Products");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductVariantViewModel model)
        {
            try
            {
                ModelState.Remove("ImageFile");

                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                    TempData["Error"] = "Please fill all required fields correctly";
                    return View(model);
                }

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found";
                    return RedirectToAction("Products");
                }

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                            FileUploadHelper.DeleteImage(product.ImageUrl, _webHostEnvironment);
                        product.ImageUrl = await FileUploadHelper.UploadImage(model.ImageFile, _webHostEnvironment, "products");
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogError(imgEx, "Error uploading new image");
                        TempData["Warning"] = "Product updated but image upload failed: " + imgEx.Message;
                    }
                }

                product.Name = model.Name?.Trim();
                product.CategoryId = model.CategoryId;
                product.Description = model.Description?.Trim();
                product.Price = model.Price;
                product.StockQuantity = model.StockQuantity;
                product.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                if (model.Variants != null && model.Variants.Any())
                {
                    var existingVariants = await _context.ProductVariants
                        .Where(v => v.ProductId == product.ProductId)
                        .ToListAsync();
                    _context.ProductVariants.RemoveRange(existingVariants);

                    foreach (var vi in model.Variants)
                    {
                        _context.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = product.ProductId,
                            SizeId = vi.SizeId,
                            ColorId = vi.ColorId,
                            StockQuantity = vi.StockQuantity,
                            Price = vi.Price,
                            SKU = vi.SKU,
                            IsActive = vi.IsActive
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Product '{product.Name}' updated successfully!";
                return RedirectToAction("Products");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error updating product");
                TempData["Error"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["Error"] = $"Error updating product: {ex.Message}";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found";
                    return RedirectToAction("Products");
                }

                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    try { FileUploadHelper.DeleteImage(product.ImageUrl, _webHostEnvironment); }
                    catch (Exception imgEx) { _logger.LogWarning(imgEx, "Could not delete product image"); }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Product '{product.Name}' deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product {id}");
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction("Products");
        }

        // ================================================
        // SIZES MANAGEMENT
        // ================================================
        [HttpGet]
        public async Task<IActionResult> ManageSizes()
        {
            try
            {
                var sizes = await _context.Sizes
                    .Include(s => s.Category)
                    .OrderBy(s => s.CategoryId)
                    .ThenBy(s => s.SizeOrder)
                    .ToListAsync();

                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                return View(sizes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sizes");
                TempData["Error"] = "Error loading sizes";
                return View(new List<Size>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSize(SizeViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Sizes.Add(new Size { CategoryId = model.CategoryId, SizeName = model.SizeName, SizeOrder = model.SizeOrder, IsActive = model.IsActive });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Size created successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating size");
                TempData["Error"] = $"Error creating size: {ex.Message}";
            }
            return RedirectToAction("ManageSizes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSize(int id)
        {
            try
            {
                var size = await _context.Sizes.FindAsync(id);
                if (size != null) { _context.Sizes.Remove(size); await _context.SaveChangesAsync(); TempData["Success"] = "Size deleted successfully!"; }
                else TempData["Error"] = "Size not found";
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error deleting size {id}"); TempData["Error"] = $"Error: {ex.Message}"; }
            return RedirectToAction("ManageSizes");
        }

        // ================================================
        // COLORS MANAGEMENT
        // ================================================
        [HttpGet]
        public async Task<IActionResult> ManageColors()
        {
            try
            {
                var colors = await _context.Colors
                    .Include(c => c.Category)
                    .OrderBy(c => c.CategoryId)
                    .ThenBy(c => c.ColorName)
                    .ToListAsync();

                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                return View(colors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading colors");
                TempData["Error"] = "Error loading colors";
                return View(new List<Color>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateColor(ColorViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Colors.Add(new Color { CategoryId = model.CategoryId, ColorName = model.ColorName, ColorCode = model.ColorCode, IsActive = model.IsActive });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Color created successfully!";
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error creating color"); TempData["Error"] = $"Error: {ex.Message}"; }
            return RedirectToAction("ManageColors");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteColor(int id)
        {
            try
            {
                var color = await _context.Colors.FindAsync(id);
                if (color != null) { _context.Colors.Remove(color); await _context.SaveChangesAsync(); TempData["Success"] = "Color deleted!"; }
                else TempData["Error"] = "Color not found";
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error deleting color {id}"); TempData["Error"] = $"Error: {ex.Message}"; }
            return RedirectToAction("ManageColors");
        }

        // ================================================
        // ORDERS MANAGEMENT
        // ================================================
        public async Task<IActionResult> Orders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = $"Error loading orders: {ex.Message}";
                return View(new List<Order>());
            }
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails).ThenInclude(od => od.Product).ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    TempData["Error"] = "Order not found";
                    return RedirectToAction("Orders");
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order {id}");
                TempData["Error"] = $"Error loading order details: {ex.Message}";
                return RedirectToAction("Orders");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = status;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Order status updated to {status} successfully!";
                }
                else TempData["Error"] = "Order not found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order status for {orderId}");
                TempData["Error"] = $"Error updating order status: {ex.Message}";
            }
            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // ================================================
        // CATEGORIES MANAGEMENT
        // ================================================
        public async Task<IActionResult> Categories()
        {
            try
            {
                return View(await _context.Categories.OrderBy(c => c.Name).ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["Error"] = "Error loading categories";
                return View(new List<Category>());
            }
        }

        [HttpGet]
        public IActionResult CreateCategory() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Category created successfully!";
                    return RedirectToAction("Categories");
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["Error"] = $"Error creating category: {ex.Message}";
                return View(category);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null) { TempData["Error"] = "Category not found"; return RedirectToAction("Categories"); }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading category {id}");
                TempData["Error"] = "Error loading category";
                return RedirectToAction("Categories");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Categories.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Category updated successfully!";
                    return RedirectToAction("Categories");
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                TempData["Error"] = $"Error: {ex.Message}";
                return View(category);
            }
        }

        // ================================================
        // USERS MANAGEMENT
        // ================================================
        public async Task<IActionResult> Users()
        {
            try
            {
                return View(await _context.Users.OrderByDescending(u => u.CreatedDate).ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["Error"] = "Error loading users";
                return View(new List<User>());
            }
        }

        public async Task<IActionResult> ViewUser(int? id)
        {
            if (id == null) { TempData["Error"] = "User ID not provided"; return RedirectToAction("Users"); }
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null) { TempData["Error"] = "User not found"; return RedirectToAction("Users"); }
                return View(user);
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error loading user {id}"); TempData["Error"] = "Error loading user"; return RedirectToAction("Users"); }
        }

        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null) { TempData["Error"] = "User ID not provided"; return RedirectToAction("Users"); }
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) { TempData["Error"] = "User not found"; return RedirectToAction("Users"); }
                return View(user);
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error loading user {id}"); TempData["Error"] = "Error loading user"; return RedirectToAction("Users"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, User user)
        {
            if (id != user.UserId) { TempData["Error"] = "User ID mismatch"; return RedirectToAction("Users"); }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "User updated successfully!";
                    return RedirectToAction("Users");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId)) { TempData["Error"] = "User not found"; return RedirectToAction("Users"); }
                    throw;
                }
                catch (Exception ex) { _logger.LogError(ex, $"Error updating user {id}"); TempData["Error"] = $"Error: {ex.Message}"; }
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) { TempData["Error"] = "User not found!"; return RedirectToAction("Users"); }
                if (user.Role == "Admin") { TempData["Error"] = "Cannot delete admin users!"; return RedirectToAction("Users"); }
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "User deleted successfully!";
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error deleting user {id}"); TempData["Error"] = "An error occurred while deleting the user."; }
            return RedirectToAction("Users");
        }

        private bool UserExists(int id) => _context.Users.Any(e => e.UserId == id);
    }
}