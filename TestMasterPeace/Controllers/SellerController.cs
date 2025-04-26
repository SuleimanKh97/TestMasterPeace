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
            try
            {
                // Log all claims for debugging
                Console.WriteLine("User claims:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }

                // First try standard .NET Core claim type
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                
                // If not found, try the JWT specific format
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.Claims.FirstOrDefault(c => 
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                }
                
                // If still not found, try other common claim types
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "nameidentifier")?.Value;
                }
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                }
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                }

                Console.WriteLine($"Found user ID claim: {userIdClaim}");
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    Console.WriteLine("ERROR: No user ID claim found in token");
                    throw new UnauthorizedAccessException("Seller ID not found in token.");
                }

                if (long.TryParse(userIdClaim, out long userId))
                {
                    Console.WriteLine($"Successfully parsed user ID: {userId}");
                    return userId;
                }
                
                Console.WriteLine($"ERROR: Could not parse '{userIdClaim}' as a long integer");
                throw new UnauthorizedAccessException("Invalid seller ID format in token.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetCurrentSellerId: {ex.Message}");
                throw new UnauthorizedAccessException($"Error retrieving seller ID: {ex.Message}");
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var sellerUsername = User.Identity.Name;
            var sellerId = GetCurrentSellerId(); // No await needed

            try
            {
                // ابحث عن البائع مع تضمين المنتجات
                var seller = await _dbContext.Users
                    .Include(u => u.Products)
                    .FirstOrDefaultAsync(u => u.Username == sellerUsername);

                if (seller == null || seller.Role != "Seller")
                    return Unauthorized(new { message = "Access denied" });

                // احسب المنتجات المباعة وغير المباعة
                int soldProducts = seller.Products.Count(p => p.IsSold);
                int availableProducts = seller.Products.Count(p => !p.IsSold);

                // احصل على طلبات البائع (من خلال منتجاته)
                var orderItems = await _dbContext.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Product != null && oi.Product.SellerId == sellerId && oi.Order != null)
                    .ToListAsync();

                // احسب إجمالي الطلبات الفريدة
                var uniqueOrderIds = orderItems
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .Count();

                // احسب إجمالي الإيرادات
                decimal totalRevenue = orderItems
                    .Where(oi => oi.Order.Status.ToLower() != "cancelled")
                    .Sum(oi => oi.Price * oi.Quantity);

                // احسب الطلبات حسب الحالة
                var ordersByStatus = orderItems
                    .GroupBy(oi => oi.Order.Status.ToLower())
                    .Select(g => new { status = g.Key, count = g.Select(oi => oi.OrderId).Distinct().Count() })
                    .ToDictionary(x => x.status, x => x.count);

                // احسب المنتجات الأكثر مبيعًا
                var topProducts = orderItems
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new {
                        productId = g.Key,
                        productName = g.First().Product.Name,
                        totalSold = g.Sum(oi => oi.Quantity),
                        totalRevenue = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderByDescending(x => x.totalSold)
                    .Take(5)
                    .ToList();

                // إحصائيات المبيعات في الأشهر الأخيرة
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var monthlySales = orderItems
                    .Where(oi => oi.Order.CreatedAt >= sixMonthsAgo && oi.Order.Status.ToLower() != "cancelled")
                    .GroupBy(oi => new { month = oi.Order.CreatedAt.Value.Month, year = oi.Order.CreatedAt.Value.Year })
                    .Select(g => new {
                        month = g.Key.month,
                        year = g.Key.year,
                        monthName = new DateTime(g.Key.year, g.Key.month, 1).ToString("MMM"),
                        totalSales = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderBy(x => x.year)
                    .ThenBy(x => x.month)
                    .ToList();

                return Ok(new {
                    totalProducts = seller.Products.Count,
                    soldProducts,
                    availableProducts,
                    totalOrders = uniqueOrderIds,
                    totalRevenue,
                    ordersByStatus,
                    topProducts,
                    monthlySales
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDashboard: {ex.Message}");
                return StatusCode(500, new { message = "حدث خطأ أثناء تحميل لوحة المعلومات" });
            }
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

        [HttpGet("orders/summary")]
        [Microsoft.AspNetCore.Cors.EnableCors("_myAllowSpecificOrigins")]
        public async Task<IActionResult> GetOrdersSummary()
        {
            try
            {
                Console.WriteLine("Starting GetOrdersSummary method");
                var username = User.Identity?.Name;
                Console.WriteLine($"User authenticated as: {username}");
                
                long sellerId;
                try 
                {
                    sellerId = GetCurrentSellerId();
                    Console.WriteLine($"Seller ID retrieved: {sellerId}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Authorization error: {ex.Message}");
                    return Unauthorized(new { message = ex.Message });
                }

                // First check if there are any products for this seller
                var sellerProducts = await _dbContext.Products
                    .Where(p => p.SellerId == sellerId)
                    .ToListAsync();
                    
                Console.WriteLine($"Found {sellerProducts.Count} products for seller {sellerId}");
                
                // Default values for response - ensure all properties are initialized
                var emptyDict = new Dictionary<string, int>
                {
                    { "pending", 0 },
                    { "processing", 0 },
                    { "shipped", 0 },
                    { "delivered", 0 },
                    { "cancelled", 0 }
                };

                // Define the structure for recentOrders
                var emptyRecentOrders = new List<object>();
                
                // Define the structure for orderTrends
                var emptyOrderTrends = Enumerable.Range(1, 6)
                    .Select(i => 
                    {
                        var date = DateTime.Now.AddMonths(-6 + i);
                        return new
                        {
                            month = date.Month,
                            year = date.Year,
                            monthName = date.ToString("MMM"),
                            orderCount = 0,
                            totalRevenue = 0m
                        };
                    })
                    .ToList<object>();
                
                if (sellerProducts.Count == 0)
                {
                    Console.WriteLine("No products found for this seller, returning empty summary");
                    return Ok(new {
                        totalOrders = 0,
                        ordersByStatus = emptyDict,
                        recentOrders = emptyRecentOrders,
                        orderTrends = emptyOrderTrends,
                        noProducts = true
                    });
                }

                // Get all order items for this seller's products
                Console.WriteLine("Fetching order items from database...");
                var orderItems = await _dbContext.OrderItems
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o.User)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Product != null && oi.Product.SellerId == sellerId && oi.Order != null)
                    .ToListAsync();

                Console.WriteLine($"Retrieved {orderItems.Count} order items for seller {sellerId}");
                
                if (orderItems.Count == 0)
                {
                    Console.WriteLine("No order items found, returning empty summary");
                    return Ok(new {
                        totalOrders = 0,
                        ordersByStatus = emptyDict,
                        recentOrders = emptyRecentOrders,
                        orderTrends = emptyOrderTrends,
                        noOrders = true
                    });
                }

                // Get order count by status
                Console.WriteLine("Calculating order status counts");
                var ordersByStatus = orderItems
                    .Where(oi => oi.Order != null && !string.IsNullOrEmpty(oi.Order.Status))
                    .GroupBy(oi => oi.Order.Status.ToLower())
                    .Select(g => new { 
                        status = g.Key, 
                        count = g.Select(oi => oi.OrderId).Distinct().Count() 
                    })
                    .ToDictionary(x => x.status, x => x.count);

                // Add any missing status types with zero counts
                foreach (var status in emptyDict.Keys.Where(k => !ordersByStatus.ContainsKey(k)))
                {
                    ordersByStatus[status] = 0;
                }

                // Get total count of unique orders
                var totalOrders = orderItems
                    .Where(oi => oi.OrderId.HasValue)
                    .Select(oi => oi.OrderId.Value)
                    .Distinct()
                    .Count();
                Console.WriteLine($"Total unique orders: {totalOrders}");

                // Get recent orders (last 5) - convert to list of objects
                Console.WriteLine("Building recent orders summary");
                var recentOrdersData = orderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt.HasValue)
                    .OrderByDescending(oi => oi.Order.CreatedAt)
                    .Select(oi => oi.Order)
                    .Distinct()
                    .Take(5)
                    .Select(o => new {
                        orderId = o.Id,
                        date = o.CreatedAt,
                        status = o.Status,
                        totalAmount = orderItems.Where(oi => oi.OrderId == o.Id).Sum(oi => oi.Price * oi.Quantity),
                        buyerName = o.User != null ? o.User.Username : "Unknown"
                    })
                    .ToList();

                // Convert to list of objects
                var recentOrders = recentOrdersData.Select(x => (object)x).ToList();

                // Get order trends by month (last 6 months)
                Console.WriteLine("Calculating monthly trends");
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                
                // Get existing data
                var monthlyDataDict = orderItems
                    .Where(oi => oi.Order != null && oi.Order.CreatedAt.HasValue && oi.Order.CreatedAt >= sixMonthsAgo)
                    .GroupBy(oi => new { month = oi.Order.CreatedAt.Value.Month, year = oi.Order.CreatedAt.Value.Year })
                    .Select(g => new {
                        month = g.Key.month,
                        year = g.Key.year,
                        monthName = new DateTime(g.Key.year, g.Key.month, 1).ToString("MMM"),
                        orderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                        totalRevenue = g.Sum(oi => oi.Price * oi.Quantity)
                    })
                    .ToDictionary(x => new { x.month, x.year });
                
                // Create a list to store the trend data
                var orderTrends = new List<object>();
                
                // Fill in all months
                for (int i = 0; i < 6; i++)
                {
                    var date = DateTime.Now.AddMonths(-5 + i);
                    var key = new { month = date.Month, year = date.Year };
                    
                    if (monthlyDataDict.TryGetValue(key, out var monthData))
                    {
                        orderTrends.Add(monthData);
                    }
                    else
                    {
                        orderTrends.Add(new
                        {
                            month = date.Month,
                            year = date.Year,
                            monthName = date.ToString("MMM"),
                            orderCount = 0,
                            totalRevenue = 0m
                        });
                    }
                }
                
                // Sort the trends
                orderTrends = orderTrends
                    .OrderBy(o => ((dynamic)o).year)
                    .ThenBy(o => ((dynamic)o).month)
                    .ToList();

                Console.WriteLine($"Order trends has {orderTrends.Count} entries");
                Console.WriteLine("GetOrdersSummary completed successfully");
                
                // Construct the response
                var response = new {
                    totalOrders = totalOrders,
                    ordersByStatus = ordersByStatus,
                    recentOrders = recentOrders,
                    orderTrends = orderTrends
                };
                
                Console.WriteLine($"Response contains: {totalOrders} orders, {ordersByStatus.Count} status types, {recentOrders.Count} recent orders, {orderTrends.Count} monthly trends");
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetOrdersSummary: {ex.Message}");
                Console.WriteLine($"Exception details: {ex}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error retrieving orders summary", error = ex.Message });
            }
        }

        [HttpGet("test")]
        [Microsoft.AspNetCore.Cors.EnableCors("_myAllowSpecificOrigins")]
        public IActionResult Test()
        {
            return Ok(new { message = "API is working", timestamp = DateTime.Now });
        }

        [HttpGet("whoami")]
        [Microsoft.AspNetCore.Cors.EnableCors("_myAllowSpecificOrigins")]
        public IActionResult WhoAmI()
        {
            try
            {
                var identity = new
                {
                    IsAuthenticated = User.Identity.IsAuthenticated,
                    Name = User.Identity.Name,
                    Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList()
                };

                return Ok(new { 
                    message = "Authentication check", 
                    identity = identity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking identity", error = ex.Message });
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
