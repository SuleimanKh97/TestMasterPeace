using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.DTOs.ProductsDTOs;
using TestMasterPeace.Models;
using TestMasterPeace.Services;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestMasterPeace.Controllers.TestMasterPeace.DTOs.SellerDTOs;

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class SellerController : ControllerBase
    {
        private readonly MasterPeiceContext _dbContext;

        public SellerController(MasterPeiceContext dbContext)
        {
            _dbContext = dbContext;
        }

        private long GetCurrentSellerId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Seller ID not found or invalid in token.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var sellerUsername = User.Identity.Name;
            var seller = await _dbContext.Users.Include(u => u.Products).Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Username == sellerUsername);

            if (seller == null || seller.Role != "Seller")
                return Unauthorized(new { message = "Access denied" });

            return Ok(new
            {
                totalProducts = seller.Products.Count,
                totalOrders = seller.Orders.Count,
                totalRevenue = seller.Orders.Sum(o => o.TotalPrice),
            });
        }

        // GET Seller Products
        [HttpGet("products")]
        public async Task<IActionResult> GetSellerProducts()
        {
            var sellerUsername = User.Identity?.Name; // Use null-conditional operator
            if (string.IsNullOrEmpty(sellerUsername))
            {
                return Unauthorized(new { message = "Cannot identify seller from token." });
            }

            // Query products where the Seller's Username matches
            // This assumes you have navigation property setup: Product -> Seller -> Username
            // Adjust the query based on your actual DbContext and relationships
            var products = await _dbContext.Products
                                       .Where(p => p.Seller != null && p.Seller.Username == sellerUsername)
                                       .Select(p => new
                                       {
                                           p.Id,
                                           p.Name, 
                                           p.Description,
                                           p.Price,
                                           p.CategoryId,
                                           p.Img,
                                           p.CreatedAt,
                                           p.SellerId,
                                           p.IsSold
                                       })
                                       .ToListAsync();

            return Ok(products);
        }

        // GET Single Seller Product by ID
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var sellerUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(sellerUsername))
            {
                return Unauthorized(new { message = "Cannot identify seller from token." });
            }

            // Find the product by ID
            // Include Category or other related data if needed by the form
            var product = await _dbContext.Products
                                       // .Include(p => p.Category) // Example include
                                       .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            // Verify the product belongs to the current seller
            // This requires either product.SellerId or product.Seller.Username
            // Assuming product.Seller.Username exists (adjust if using SellerId)
            var productSeller = await _dbContext.Users.FindAsync(product.SellerId);
            if (productSeller?.Username != sellerUsername)
            {
                 // Or check product.Seller.Username if navigation property exists and loaded
                 // if (product.Seller?.Username != sellerUsername) { ... }
                 return Unauthorized(new { message = "You are not authorized to view this product." });
            }
            /* Alternative check using SellerId directly if navigation property not used:
               var seller = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == sellerUsername);
               if (seller == null || product.SellerId != seller.Id)
               {
                    return Unauthorized(new { message = "You are not authorized to view this product." });
               }
            */

            // Return the product data with IsSold property
            var productData = new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.CategoryId,
                product.Img,
                product.CreatedAt,
                product.SellerId,
                product.IsSold
            };

            return Ok(productData);
        }

        [HttpPost("products")]
        public async Task<IActionResult> AddProduct(CreateProductRequest newProduct)
        {
            var sellerUsername = User.Identity.Name;
            var seller = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == sellerUsername);

            if (seller == null || seller.Role != "Seller")
                return Unauthorized(new { message = "Access denied" });
            var newproduct = new Product
            {
                Name = newProduct.Name,
                Description = newProduct.Description,
                Price = newProduct.Price,
                CategoryId = newProduct.CategoryId,
                Img = newProduct.Img,
                CreatedAt = DateTime.Now
            };
            newproduct.SellerId = seller.Id;

            await _dbContext.Products.AddAsync(newproduct);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product added successfully" });
        }
        [HttpPut("products/{id}")]
        public async Task<IActionResult> EditProduct(int id, CreateProductRequest updatedProduct)
        {
            var sellerUsername = User.Identity.Name;
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id && p.Seller.Username == sellerUsername);

            if (product == null)
                return NotFound(new { message = "Product not found or unauthorized" });

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.CreatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Product updated successfully" });
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var sellerUsername = User.Identity.Name;
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id && p.Seller.Username == sellerUsername);

            if (product == null)
                return NotFound(new { message = "Product not found or unauthorized" });

            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var sellerUsername = User.Identity.Name;
            var orders = await _dbContext.Orders
                .Where(o => o.User.Username == sellerUsername)
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("MyOrderItems")]
        public async Task<IActionResult> GetMyOrderItems()
        {
            long sellerId;
            try { sellerId = GetCurrentSellerId(); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

            try
            {
                var sellerOrderItems = await _dbContext.OrderItems
                    // Ensure necessary Includes are present and correct
                    .Include(oi => oi.Product) // For ProductName, ImageUrl, Seller check
                    .Include(oi => oi.Order)   // Base Order object needed
                        .ThenInclude(o => o.User) // Include the Buyer (User) associated with the Order
                    .Where(oi => oi.Product != null 
                                 && oi.Product.SellerId == sellerId 
                                 && oi.Order != null // Ensure Order exists
                                 && oi.Order.Status.ToLower() != "cancelled") // Exclude cancelled orders
                    .OrderByDescending(oi => oi.Order.CreatedAt)
                    .Select(oi => new SellerOrderItemDTO // Map to the DTO
                    {
                        // Existing Item details
                        OrderItemId = oi.Id,
                        OrderId = oi.OrderId.Value,
                        OrderDate = oi.Order.CreatedAt, // Order date from the included Order
                        OrderStatus = oi.Order.Status,  // Order status from the included Order
                        ProductId = oi.ProductId.Value,
                        ProductName = oi.Product.Name, // From included Product
                        Quantity = oi.Quantity,
                        PricePerItem = oi.Price,
                        ImageUrl = oi.Product.Img,   // From included Product (using Img property)
                        
                        // --- Populate Buyer and Shipping Info --- 
                        // Access User info via included Order.User
                        BuyerUsername = oi.Order.User != null ? oi.Order.User.Username : "Unknown Buyer", 
                        // Access Shipping info directly from the included Order
                        ShippingPhoneNumber = oi.Order.ShippingPhoneNumber, 
                        ShippingAddressLine1 = oi.Order.ShippingAddressLine1,
                        ShippingAddressLine2 = oi.Order.ShippingAddressLine2,
                        ShippingCity = oi.Order.ShippingCity
                        // ---------------------------------------
                    })
                    .ToListAsync();

                // Log the result count before returning (for debugging)
                Console.WriteLine($"Found {sellerOrderItems.Count} order items for seller {sellerId}"); 

                return Ok(sellerOrderItems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching order items for seller {sellerId}: {ex}");
                // Log the full exception details for better debugging
                Console.WriteLine(ex.ToString()); 
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching your order items.");
            }
        }

        // PUT: /Seller/orders/{orderId}/status
        [HttpPut("orders/{orderId}/status")]
        public async Task<IActionResult> UpdateOrderStatus(long orderId, [FromBody] UpdateOrderStatusRequestDTO request)
        {
            long sellerId;
            try { sellerId = GetCurrentSellerId(); } 
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

            if (request == null || string.IsNullOrEmpty(request.NewStatus))
            {
                return BadRequest(new { message = "New status is required." });
            }

            var validNewStatuses = new[] { "shipped", "cancelled" }; // Allowed new statuses by seller
            var newStatusLower = request.NewStatus.ToLower();

            if (!validNewStatuses.Contains(newStatusLower))
            {
                return BadRequest(new { message = "Invalid target status." });
            }

            try
            {
                // Find the order
                var order = await _dbContext.Orders
                    .Include(o => o.OrderItems) // Include items to check ownership
                        .ThenInclude(oi => oi.Product) 
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }

                // Verify that at least one item in the order belongs to this seller
                bool sellerInvolved = order.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId);
                if (!sellerInvolved)
                {
                     return Forbid(); // Or BadRequest("You are not involved in this order.");
                }

                // Check if the status transition is allowed
                var currentStatusLower = order.Status.ToLower();
                bool transitionAllowed = false;
                if (newStatusLower == "shipped" && currentStatusLower == "processing")
                {
                    transitionAllowed = true;
                }
                else if (newStatusLower == "cancelled" && currentStatusLower == "pending")
                {
                    transitionAllowed = true;
                }
                // Add more allowed transitions if needed

                if (!transitionAllowed)
                {
                     return BadRequest(new { message = $"Cannot change order status from '{order.Status}' to '{request.NewStatus}'." });
                }

                // Update the order status
                order.Status = request.NewStatus; // Use the provided casing or normalize it (e.g., ToUpperCamelCase)

                // Optionally: Add logic for stock adjustment on cancellation, notifications, etc.

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Order status updated successfully.", orderId = order.Id, newStatus = order.Status });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating status for order {orderId} by seller {sellerId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the order status.");
            }
        }
    }

    namespace TestMasterPeace.DTOs.SellerDTOs
    {
        public class SellerOrderItemDTO
        {
            public long OrderItemId { get; set; }
            public long OrderId { get; set; }
            public DateTime? OrderDate { get; set; }
            public string OrderStatus { get; set; }
            public long ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal PricePerItem { get; set; }
            public string? ImageUrl { get; set; }

            // --- Add Buyer and Shipping Info --- 
            public string BuyerUsername { get; set; } // Username of the buyer
            public string ShippingPhoneNumber { get; set; }
            public string ShippingAddressLine1 { get; set; }
            public string? ShippingAddressLine2 { get; set; }
            public string ShippingCity { get; set; }
            // -----------------------------------
        }

        // --- DTO for Update Order Status Request ---
        public class UpdateOrderStatusRequestDTO
        {
            [System.ComponentModel.DataAnnotations.Required]
            public string NewStatus { get; set; }
        }
    }
}
