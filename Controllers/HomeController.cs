using BrandsStore.Data;
using BrandsStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BrandsStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================================
        // HOME PAGE
        // ================================================
        public async Task<IActionResult> Index()
        {
            try
            {
                // Featured products (model passed to view)
                var featuredProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(8)
                    .ToListAsync();

                // New arrivals
                ViewBag.NewArrivals = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(4)
                    .ToListAsync();

                // Best sellers (low stock = high sales proxy)
                ViewBag.BestSellers = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.StockQuantity > 0)
                    .OrderBy(p => p.StockQuantity)
                    .Take(4)
                    .ToListAsync();

                // ? FIX: renamed from ViewBag.Categories ? ViewBag.HomeCategories
                // (matches the variable name used in Home/Index.cshtml)
                ViewBag.HomeCategories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // ? NEW: Active offers for the hero carousel
                ViewBag.HeroOffers = await _context.Offers
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.SortOrder)
                    .ThenByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return View(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                ViewBag.HomeCategories = new List<Category>();
                ViewBag.HeroOffers = new List<Offer>();
                ViewBag.NewArrivals = new List<Product>();
                ViewBag.BestSellers = new List<Product>();
                return View(new List<Product>());
            }
        }

        // ================================================
        // PRODUCTS LIST
        // ================================================
        public async Task<IActionResult> Products(int? categoryId, string search)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive);

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                    ViewBag.SelectedCategory = categoryId.Value;
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search) ||
                                            (p.Description != null && p.Description.Contains(search)));
                    ViewBag.SearchTerm = search;
                }

                var products = await query
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.SelectedCategoryId = categoryId;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products page");
                ViewBag.Categories = new List<Category>();
                return View(new List<Product>());
            }
        }

        // ================================================
        // PRODUCT DETAILS WITH VARIANTS
        // ================================================
        public async Task<IActionResult> ProductDetails(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

                if (product == null)
                {
                    _logger.LogWarning($"Product {id} not found or inactive");
                    return NotFound();
                }

                ViewBag.RelatedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == product.CategoryId &&
                               p.ProductId != id &&
                               p.IsActive)
                    .Take(4)
                    .ToListAsync();

                ViewBag.Sizes = await _context.Sizes
                    .Where(s => s.CategoryId == product.CategoryId && s.IsActive)
                    .OrderBy(s => s.SizeOrder)
                    .ToListAsync();

                ViewBag.Colors = await _context.Colors
                    .Where(c => c.CategoryId == product.CategoryId && c.IsActive)
                    .OrderBy(c => c.ColorName)
                    .ToListAsync();

                ViewBag.Variants = await _context.ProductVariants
                    .Include(v => v.Size)
                    .Include(v => v.Color)
                    .Where(v => v.ProductId == id && v.IsActive)
                    .ToListAsync();

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading product details for product {id}");
                return NotFound();
            }
        }

        // ================================================
        // CHECK USER (debug)
        // ================================================
        public async Task<IActionResult> CheckUser()
        {
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
            ViewBag.UserName = User.Identity.Name;

            if (User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    ViewBag.UserId = user.UserId;
                    ViewBag.FullName = user.FullName;
                    ViewBag.Email = user.Email;
                    ViewBag.Role = user.Role;
                    ViewBag.IsActive = user.IsActive;
                }
                else
                {
                    ViewBag.UserNotFound = true;
                }
            }

            return View();
        }

        // ================================================
        // STATIC PAGES
        // ================================================
        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}