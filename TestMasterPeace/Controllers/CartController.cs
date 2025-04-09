using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TestMasterPeace.Models;
using TestMasterPeace.DTOs.CartDTOs; // We will define DTOs shortly

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all cart operations
    public class CartController : ControllerBase
    {
        private readonly MasterPeiceContext _context;

        public CartController(MasterPeiceContext context)
        {
            _context = context;
        }

        private long GetUserId()
        {
            // Helper method to get user ID from claims
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            // It's better to use NameIdentifier if it stores the numeric ID
            // If NameIdentifier stores username, use another claim like "UserId" if you added one
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            // Handle cases where ID is not found or not a valid long - throw or return error
            throw new UnauthorizedAccessException("User ID not found or invalid in token.");
        }

        // GET: /Cart
        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            try
            {
                var userId = GetUserId();
                var cartItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product) // Include product details
                    .Select(c => new CartItemResponseDTO
                    {
                        ProductId = c.ProductId ?? 0,
                        ProductName = c.Product != null ? c.Product.Name : "Unknown Product",
                        Quantity = c.Quantity,
                        Price = c.Product != null ? c.Product.Price : 0,
                        ImageUrl = c.Product != null ? c.Product.Img : null // Assuming Product model has Img
                    })
                    .ToListAsync();

                return Ok(cartItems);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while fetching cart items.");
            }
        }

        // POST: /Cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(AddItemToCartRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetUserId();

                // Check if product exists
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found." });
                }

                // Check if item already in cart
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

                if (cartItem != null)
                {
                    // Update quantity if item exists
                    cartItem.Quantity += request.Quantity;
                }
                else
                {
                    // Add new item if it doesn't exist
                    cartItem = new Cart
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                // Optionally return the updated cart or the added item details
                // For simplicity, return Ok for now
                return Ok(new { message = "Item added to cart successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the item.");
            }
        }

        // PUT: /Cart/{productId}
        [HttpPut("{productId}")]
        public async Task<IActionResult> UpdateCartItem(long productId, UpdateCartItemQuantityRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

             if (request.Quantity <= 0)
            {
                 // Treat quantity 0 or less as removal
                 return await RemoveFromCart(productId);
            }

            try
            {
                var userId = GetUserId();
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (cartItem == null)
                {
                    return NotFound(new { message = "Item not found in cart." });
                }

                cartItem.Quantity = request.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart item quantity updated." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating quantity.");
            }
        }

        // DELETE: /Cart/{productId}
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromCart(long productId)
        {
            try
            {
                var userId = GetUserId();
                var cartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (cartItem == null)
                {
                    // It's okay if item not found, maybe already removed
                    return NoContent(); 
                }

                _context.Carts.Remove(cartItem);
                await _context.SaveChangesAsync();

                return NoContent(); // Success, no content to return
            }
             catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing the item.");
            }
        }

        // DELETE: /Cart 
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
             try
            {
                var userId = GetUserId();
                var userCartItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (userCartItems.Any())
                {
                     _context.Carts.RemoveRange(userCartItems);
                     await _context.SaveChangesAsync();
                }
               
                return NoContent(); // Success, no content to return
            }
             catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while clearing the cart.");
            }
        }
    }
}

// --- DTO Definitions --- 
// You should ideally place these in a separate DTOs/CartDTOs folder/namespace

namespace TestMasterPeace.DTOs.CartDTOs
{
    public class AddItemToCartRequestDTO
    {
        [System.ComponentModel.DataAnnotations.Required]
        public long ProductId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemQuantityRequestDTO
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class CartItemResponseDTO
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        // Add other product details if needed
    }
} 