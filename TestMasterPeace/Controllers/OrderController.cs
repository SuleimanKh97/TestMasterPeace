using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestMasterPeace.Models;
using TestMasterPeace.DTOs.OrderDTOs; // Define DTOs below
using TestMasterPeace.DTOs.CheckoutDTOs; // Add namespace for new DTO
using System.ComponentModel.DataAnnotations;

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
        
        // POST: /Order - Handles initial order creation and COD
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDTO requestData)
        {
            long userId;
            try { userId = GetUserId(); } catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

            if (requestData == null || string.IsNullOrEmpty(requestData.PaymentMethod))
            {
                return BadRequest(new { message = "Payment method is required." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cartItems = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any()) { return BadRequest(new { message = "Cart is empty." }); }

            var itemsBySeller = cartItems
                .Where(item => item.Product?.SellerId != null)
                .GroupBy(item => item.Product.SellerId.Value);

            if (!itemsBySeller.Any()) { return BadRequest(new { message = "No valid items with sellers found in cart." }); }

            var createdOrdersInfo = new List<object>();
            bool requiresPaymentSimulation = !requestData.PaymentMethod.Equals("CashOnDelivery", StringComparison.OrdinalIgnoreCase);
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var sellerGroup in itemsBySeller)
                {
                    var sellerCartItems = sellerGroup.ToList();
                    decimal sellerTotalAmount = sellerCartItems.Sum(item => item.Quantity * item.Product.Price);

                    // Determine initial status and if transaction record is needed now
                    string initialStatus = requiresPaymentSimulation ? "Pending" : "Processing";
                    Transaction? orderTransaction = null;

                    if (!requiresPaymentSimulation)
                    {
                        // For CashOnDelivery, set status and create completed transaction now
                        initialStatus = "Processing"; // Or keep Pending based on workflow
                        orderTransaction = new Transaction
                        {
                             Amount = sellerTotalAmount,
                             PaymentMethod = requestData.PaymentMethod,
                             TransactionDate = DateTime.UtcNow,
                             // OrderId will be set after saving Order
                        };
                         await _context.Transactions.AddAsync(orderTransaction);
                         // Note: SaveChanges for Transaction will happen after Order is saved
                    }
                    // For simulated payments, status remains Pending, no transaction record yet.

                    var newOrder = new Order
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        TotalPrice = sellerTotalAmount,
                        Status = initialStatus,
                        
                        // --- Assign Shipping Info --- 
                        ShippingPhoneNumber = requestData.ShippingPhoneNumber,
                        ShippingAddressLine1 = requestData.ShippingAddressLine1,
                        ShippingAddressLine2 = requestData.ShippingAddressLine2, // Handles null
                        ShippingCity = requestData.ShippingCity
                        // --------------------------
                    };
                    
                    // Correct: Add the transaction to the collection if it exists
                    if (orderTransaction != null)
                    {
                        newOrder.Transactions.Add(orderTransaction); 
                    }

                    await _context.Orders.AddAsync(newOrder);
                    await _context.SaveChangesAsync(); // Save to get OrderId
                    long orderId = newOrder.Id;
                    
                    createdOrdersInfo.Add(new { orderId = orderId, sellerId = sellerGroup.Key });

                    var orderItems = sellerCartItems.Select(cartItem => new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = cartItem.ProductId.Value,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price
                    }).ToList();
                    await _context.OrderItems.AddRangeAsync(orderItems);
                    
                    // For cash on delivery orders, mark the products as sold immediately
                    if (!requiresPaymentSimulation)
                    {
                        foreach (var cartItem in sellerCartItems)
                        {
                            if (cartItem.Product != null)
                            {
                                cartItem.Product.IsSold = true;
                            }
                        }
                    }
                }

                // Only clear cart if it was NOT a simulated payment (cart needed for payment confirmation)
                if (!requiresPaymentSimulation)
                {
                     _context.Carts.RemoveRange(cartItems);
                }

                await _context.SaveChangesAsync(); // Saves OrderItems, Transaction OrderId update, and Cart removal
                await transaction.CommitAsync();

                return Ok(new {
                     message = requiresPaymentSimulation ? "Order created. Proceed to payment simulation." : "Order(s) created successfully!",
                     orderIds = createdOrdersInfo.Select(o => ((dynamic)o).orderId).ToList(),
                     requiresPayment = requiresPaymentSimulation,
                     // Return the first orderId for redirection if simulating payment for simplicity
                     // A more robust solution might return info for all orders requiring payment.
                     primaryOrderId = requiresPaymentSimulation ? createdOrdersInfo.Select(o => ((dynamic)o).orderId).FirstOrDefault() : (long?)null
                      });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error in CreateOrder: {ex}");
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

                if (order.Status.ToLower() != "pending" && order.Status.ToLower() != "processing")
                {
                    return BadRequest(new { message = "لا يمكن إلغاء الطلب في حالته الحالية." });
                }

                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
                return Ok(new { message = "تم إلغاء الطلب بنجاح." });
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                Console.WriteLine($"Error cancelling order {orderId} for user {userId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling the order.");
            }
        }
        
        // --- New Endpoint for Buyer --- 
        // PUT: /Order/{orderId}/mark-delivered
        [HttpPut("{orderId}/mark-delivered")]
        public async Task<IActionResult> MarkOrderAsDelivered(long orderId)
        {
            long userId;
            try { userId = GetUserId(); } catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { message = "لم يتم العثور على الطلب." });
            }

            // Allow marking as delivered only if the order is 'Shipped'
            if (order.Status?.ToLower() != "shipped")
            {
                return BadRequest(new { message = "لا يمكن تأكيد استلام طلب لم يتم شحنه بعد." });
            }

            order.Status = "Delivered";
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تأكيد استلام الطلب بنجاح!" });
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

// --- DTO Definition for Create Order Request ---
namespace TestMasterPeace.DTOs.CheckoutDTOs
{
    public class CreateOrderRequestDTO
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string PaymentMethod { get; set; }

        // --- Add Shipping Info DTO Properties ---
        [Required]
        [StringLength(15)]
        public string ShippingPhoneNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingAddressLine1 { get; set; }

        [StringLength(100)]
        public string? ShippingAddressLine2 { get; set; } // Optional

        [Required]
        [StringLength(50)]
        public string ShippingCity { get; set; }
        // ----------------------------------------
    }
} 