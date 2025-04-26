using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestMasterPeace.Models;
using TestMasterPeace.DTOs.PaymentDTOs; // Define DTOs below

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize] // Require user to be logged in
    public class PaymentController : ControllerBase
    {
        private readonly MasterPeiceContext _context;

        public PaymentController(MasterPeiceContext context)
        {
            _context = context;
        }

        private long GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId)) { return userId; }
            throw new UnauthorizedAccessException("User ID not found or invalid in token.");
        }

        // POST: /Payment/InitiateSimulation
        [HttpPost("InitiateSimulation")]
        public async Task<IActionResult> InitiatePaymentSimulation([FromBody] InitiatePaymentRequestDTO request)
        {
             long userId;
            try { userId = GetUserId(); } catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

             if (request == null || request.OrderId <= 0)
            {
                return BadRequest(new { message = "Order ID is required." });
            }

            // Verify the order belongs to the user and is in Pending state
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);

            if (order == null) { return NotFound(new { message = "Order not found or does not belong to user." }); }
            if (order.Status.ToLower() != "pending") { return BadRequest(new { message = "Payment can only be initiated for pending orders." }); }

            // In a real scenario, you'd interact with the payment gateway SDK here
            // For simulation, we just return a URL to our simulation page
            // We could also update order status to 'AwaitingPayment' here if desired
            // order.Status = "AwaitingPayment";
            // await _context.SaveChangesAsync();

            var simulationUrl = $"/simulate-payment?orderId={order.Id}&amount={order.TotalPrice}"; // Frontend route
            
            return Ok(new { simulationUrl });
        }

        // POST: /Payment/ConfirmSimulation
        [HttpPost("ConfirmSimulation")]
        public async Task<IActionResult> ConfirmPaymentSimulation([FromBody] ConfirmPaymentRequestDTO request)
        {
            long userId;
            try { userId = GetUserId(); } catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

             if (request == null || request.OrderId <= 0)
            {
                return BadRequest(new { message = "Order ID and status are required." });
            }
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find the order again, ensure it belongs to user and is Pending/AwaitingPayment
                 var order = await _context.Orders
                    .Include(o => o.OrderItems) // Include items if needed for stock or other logic
                        .ThenInclude(oi => oi.Product) // Include the products to mark them as sold
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
                 
                 if (order == null) { return NotFound(new { message = "Order not found or does not belong to user." }); }
                 if (order.Status.ToLower() != "pending" /* && order.Status.ToLower() != "awaitingpayment" */) 
                 { 
                     return BadRequest(new { message = "Cannot confirm payment for order in its current state." }); 
                 }

                string finalStatus;
                string message;

                if (request.Success)
                {
                    // Payment Succeeded (Simulated)
                    finalStatus = "Processing"; // Or whatever status means ready for seller
                    message = "Payment successful (simulated). Order is now processing.";

                    // Create Transaction Record
                    var paymentTransaction = new Transaction
                    {
                        OrderId = order.Id,
                        Amount = order.TotalPrice, 
                        PaymentMethod = request.PaymentMethod ?? "SimulatedCard", // Get method used
                        TransactionDate = DateTime.UtcNow,
                        // Add other details like a simulated transaction ID if needed
                    };
                    _context.Transactions.Add(paymentTransaction);
                    
                    // Mark each product in the order as sold
                    foreach (var orderItem in order.OrderItems)
                    {
                        if (orderItem.Product != null)
                        {
                            orderItem.Product.IsSold = true;
                        }
                    }
                    
                    // Now clear the user's cart items associated with THIS order
                    // (Requires linking cart items to order items or finding them again)
                    // For simplicity, let's assume cart items related to the orderId need to be found
                    // THIS IS A SIMPLIFICATION - A better way is needed if orders can be partially paid
                    var cartItems = await _context.Carts.Where(c => c.UserId == userId).ToListAsync();
                    _context.Carts.RemoveRange(cartItems);
                }
                else
                {
                    // Payment Failed (Simulated)
                    finalStatus = "PaymentFailed"; // Or "Cancelled"
                    message = "Payment failed (simulated).";
                    // Optionally: Add logic to return stock
                }

                order.Status = finalStatus;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message, orderId = order.Id, finalStatus });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error confirming payment for order {request.OrderId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during payment confirmation.");
            }
        }
    }
}

// --- DTO Definitions ---
namespace TestMasterPeace.DTOs.PaymentDTOs
{
    public class InitiatePaymentRequestDTO
    {
        [System.ComponentModel.DataAnnotations.Required]
        public long OrderId { get; set; }
        // public string PaymentMethod { get; set; } // Might be needed
    }

    public class ConfirmPaymentRequestDTO
    {
        [System.ComponentModel.DataAnnotations.Required]
        public long OrderId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public bool Success { get; set; }
        public string? PaymentMethod { get; set; } // Record the method used
    }
} 