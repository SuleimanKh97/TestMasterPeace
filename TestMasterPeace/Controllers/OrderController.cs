using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestMasterPeace.Models;
using TestMasterPeace.DTOs.OrderDTOs; // Define DTOs below

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize] // All order actions require login
    public class OrderController : ControllerBase
    {
        private readonly MasterPeiceContext _context;

        public OrderController(MasterPeiceContext context)
        {
            _context = context;
        }

        private long GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID not found or invalid in token.");
        }

        // GET: /Order/MyOrders?status=Pending
        [HttpGet("MyOrders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] string? status = null)
        {
            try
            {
                var userId = GetUserId();
                
                // Apply WHERE before ORDER BY
                var query = _context.Orders
                    .Where(o => o.UserId == userId);

                // Apply status filter if provided
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status.ToLower() == status.ToLower());
                }

                // Now apply ORDER BY and Includes
                var orders = await query
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.CreatedAt) // Assuming CreatedAt exists for order date
                    .Select(o => new OrderDetailDTO // Project to DTO
                    {
                        OrderId = o.Id,
                        OrderDate = o.CreatedAt, // Assuming CreatedAt exists
                        TotalAmount = o.TotalPrice, // Assuming TotalPrice exists
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDTO
                        {
                            ProductId = (long)oi.ProductId, // Assuming OrderItem.ProductId is non-nullable or handled
                            ProductName = oi.Product != null ? oi.Product.Name : "Unknown Product",
                            Quantity = oi.Quantity,
                            Price = oi.Price, 
                            ImageUrl = oi.Product != null ? oi.Product.Img : null 
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching orders.");
            }
        }
        
        // POST: /Order - Handles the checkout process, creating one order per seller
        [HttpPost]
        public async Task<IActionResult> CreateOrder(/* [FromBody] CreateOrderRequestDTO requestData */)
        {
            long userId;
            try { userId = GetUserId(); } catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

            // 1. Get user's cart items including Product (for SellerId)
            var cartItems = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product) 
                .ToListAsync();

            if (!cartItems.Any()) { return BadRequest(new { message = "Cart is empty." }); }

            // 2. Group cart items by SellerId (assuming Product.SellerId exists and is NOT NULL)
             var itemsBySeller = cartItems
                .Where(item => item.Product?.SellerId != null) // Ensure product and seller ID exist
                .GroupBy(item => item.Product.SellerId.Value); // Group by SellerId

            // Check if any items were excluded due to missing product/sellerId
            if (itemsBySeller.Count() != cartItems.Count)
            {
                 Console.WriteLine($"Warning: Some cart items for user {userId} were ignored due to missing product or SellerId.");
                 // Decide if this is an error or just proceed with valid items
                 if (!itemsBySeller.Any())
                 {
                     return BadRequest(new { message = "No valid items found in cart to create orders." });
                 }
            }

            var createdOrderIds = new List<long>();
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 3. Iterate through each seller group
                foreach (var sellerGroup in itemsBySeller)
                {
                    long sellerId = sellerGroup.Key;
                    var sellerCartItems = sellerGroup.ToList();

                    // 3.1 Calculate total for this seller's items
                    decimal sellerTotalAmount = sellerCartItems.Sum(item => item.Quantity * item.Product.Price);

                    // 3.2 Create a new Order for this seller
                    var newOrder = new Order
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        TotalPrice = sellerTotalAmount,
                        Status = "Pending",
                        // You could potentially add a SellerId to the Order model itself if needed,
                        // otherwise, the link is implicit through the OrderItems -> Product -> SellerId.
                    };
                    await _context.Orders.AddAsync(newOrder);
                    await _context.SaveChangesAsync(); // Save to get OrderId
                    long orderId = newOrder.Id;
                    createdOrderIds.Add(orderId);

                    // 3.3 Create OrderItems for this specific order
                    var orderItems = sellerCartItems.Select(cartItem => new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = cartItem.ProductId.Value,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price // Price at time of order
                        // Ensure OrderItem model has necessary fields
                    }).ToList();
                    await _context.OrderItems.AddRangeAsync(orderItems);
                }

                // 4. Clear the original cart items (all of them)
                _context.Carts.RemoveRange(cartItems);

                // 5. Save all changes (OrderItems creation and Cart removal)
                await _context.SaveChangesAsync();

                // 6. Commit Transaction
                await transaction.CommitAsync();

                // 7. Return success response with list of created order IDs
                return Ok(new { message = "Order(s) created successfully!", orderIds = createdOrderIds });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error creating split orders for user {userId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the order(s).");
            }
        }

        // PUT: /Order/{orderId}/cancel
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(long orderId)
        {
            long userId;
            try
            {
                userId = GetUserId();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found or does not belong to the user." });
                }

                // Define cancelable statuses (e.g., only Pending)
                var cancelableStatuses = new[] { "pending" }; // Case-insensitive comparison below

                if (!cancelableStatuses.Contains(order.Status.ToLower()))
                {
                    return BadRequest(new { message = $"Order cannot be cancelled in its current status ('{order.Status}')." });
                }

                // Update status to Cancelled
                order.Status = "Cancelled";
                // Optionally: Add logic to return stock if needed

                await _context.SaveChangesAsync();

                return Ok(new { message = "Order cancelled successfully.", orderId = order.Id, newStatus = order.Status });
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                Console.WriteLine($"Error cancelling order {orderId} for user {userId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling the order.");
            }
        }
    }
}

// --- DTO Definitions --- 
// Place in DTOs/OrderDTOs folder ideally
namespace TestMasterPeace.DTOs.OrderDTOs
{
    public class OrderDetailDTO
    {
        public long OrderId { get; set; }
        public DateTime? OrderDate { get; set; } // Changed to DateTime? as CreatedAt might be nullable
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } 
        public List<OrderItemDTO> OrderItems { get; set; }
    }

    public class OrderItemDTO
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
} 