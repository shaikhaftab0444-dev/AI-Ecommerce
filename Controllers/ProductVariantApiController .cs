using BrandsStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrandsStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductVariantApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductVariantApiController> _logger;

        public ProductVariantApiController(
            ApplicationDbContext context,
            ILogger<ProductVariantApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ProductVariantApi/GetSizesByCategory/5
        [HttpGet("GetSizesByCategory/{categoryId}")]
        public async Task<IActionResult> GetSizesByCategory(int categoryId)
        {
            try
            {
                var sizes = await _context.Sizes
                    .Where(s => s.CategoryId == categoryId && s.IsActive)
                    .OrderBy(s => s.SizeOrder)
                    .Select(s => new
                    {
                        sizeId = s.SizeId,
                        sizeName = s.SizeName,
                        sizeOrder = s.SizeOrder
                    })
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {sizes.Count} sizes for category {categoryId}");
                return Ok(sizes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sizes for category {categoryId}");
                return StatusCode(500, new { error = "Error loading sizes" });
            }
        }

        // GET: api/ProductVariantApi/GetColorsByCategory/5
        [HttpGet("GetColorsByCategory/{categoryId}")]
        public async Task<IActionResult> GetColorsByCategory(int categoryId)
        {
            try
            {
                var colors = await _context.Colors
                    .Where(c => c.CategoryId == categoryId && c.IsActive)
                    .OrderBy(c => c.ColorName)
                    .Select(c => new
                    {
                        colorId = c.ColorId,
                        colorName = c.ColorName,
                        colorCode = c.ColorCode
                    })
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {colors.Count} colors for category {categoryId}");
                return Ok(colors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving colors for category {categoryId}");
                return StatusCode(500, new { error = "Error loading colors" });
            }
        }

        // NEW: GET ONLY SIZES AND COLORS THAT HAVE VARIANTS FOR A SPECIFIC PRODUCT
        [HttpGet("GetProductVariants/{productId}")]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            try
            {
                var variants = await _context.ProductVariants
                    .Include(v => v.Size)
                    .Include(v => v.Color)
                    .Where(v => v.ProductId == productId && v.IsActive)
                    .ToListAsync();

                // Get unique sizes with stock info
                var sizes = variants
                    .Where(v => v.SizeId.HasValue)
                    .GroupBy(v => v.SizeId)
                    .Select(g => new
                    {
                        sizeId = g.Key.Value,
                        sizeName = g.First().Size.SizeName,
                        totalStock = g.Sum(v => v.StockQuantity)
                    })
                    .OrderBy(s => s.sizeName)
                    .ToList();

                // Get unique colors with stock info
                var colors = variants
                    .Where(v => v.ColorId.HasValue)
                    .GroupBy(v => v.ColorId)
                    .Select(g => new
                    {
                        colorId = g.Key.Value,
                        colorName = g.First().Color.ColorName,
                        colorCode = g.First().Color.ColorCode,
                        totalStock = g.Sum(v => v.StockQuantity)
                    })
                    .OrderBy(c => c.colorName)
                    .ToList();

                _logger.LogInformation($"Retrieved {sizes.Count} sizes and {colors.Count} colors for product {productId}");
                
                return Ok(new
                {
                    sizes = sizes,
                    colors = colors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving variants for product {productId}");
                return StatusCode(500, new { error = "Error loading product variants" });
            }
        }

        // GET SPECIFIC VARIANT STOCK BY SIZE AND COLOR
        [HttpGet("GetVariantStock/{productId}/{sizeId}/{colorId}")]
        public async Task<IActionResult> GetVariantStock(int productId, int? sizeId, int? colorId)
        {
            try
            {
                var query = _context.ProductVariants
                    .Where(v => v.ProductId == productId && v.IsActive);

                // Filter by size if provided
                if (sizeId.HasValue && sizeId.Value > 0)
                {
                    query = query.Where(v => v.SizeId == sizeId.Value);
                }
                else
                {
                    query = query.Where(v => v.SizeId == null);
                }

                // Filter by color if provided
                if (colorId.HasValue && colorId.Value > 0)
                {
                    query = query.Where(v => v.ColorId == colorId.Value);
                }
                else
                {
                    query = query.Where(v => v.ColorId == null);
                }

                var variant = await query.FirstOrDefaultAsync();

                if (variant == null)
                {
                    return Ok(new
                    {
                        isAvailable = false,
                        stockQuantity = 0,
                        price = (decimal?)null
                    });
                }

                return Ok(new
                {
                    isAvailable = variant.StockQuantity > 0,
                    stockQuantity = variant.StockQuantity,
                    price = variant.Price
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking variant stock for product {productId}");
                return StatusCode(500, new { error = "Error checking stock" });
            }
        }
    }
}