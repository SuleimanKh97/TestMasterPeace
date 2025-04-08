using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.Models;

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BuyerController : ControllerBase
    {
        private readonly MasterPeiceContext _dbContext;

        public BuyerController(MasterPeiceContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ✅ 1. عرض سلة المشتريات
        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var buyerUsername = User.Identity.Name;
            var buyer = await _dbContext.Users
                .Include(u => u.Carts)
                .ThenInclude(c => c.Product)
                .FirstOrDefaultAsync(u => u.Username == buyerUsername);

            if (buyer == null || buyer.Role != "Buyer")
                return Unauthorized(new { message = "Access denied" });

            return Ok(buyer.Carts.Select(c => new
            {
                c.Id,
                ProductName = c.Product.Name,
                c.Quantity,
                TotalPrice = c.Quantity * c.Product.Price
            }));
        }

        // ✅ 2. إضافة منتج إلى السلة
        [HttpPost("cart")]
        public async Task<IActionResult> AddToCart([FromBody] Cart cartItem)
        {
            var buyerUsername = User.Identity.Name;
            var buyer = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == buyerUsername);

            if (buyer == null || buyer.Role != "Buyer")
                return Unauthorized(new { message = "Access denied" });

            var product = await _dbContext.Products.FindAsync(cartItem.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var cart = new Cart
            {
                UserId = buyer.Id,
                ProductId = product.Id,
                Quantity = cartItem.Quantity
            };

            await _dbContext.Carts.AddAsync(cart);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product added to cart" });
        }

        // ✅ 3. إزالة منتج من السلة
        [HttpDelete("cart/{cartId}")]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var buyerUsername = User.Identity.Name;
            var cartItem = await _dbContext.Carts
                .FirstOrDefaultAsync(c => c.Id == cartId && c.User.Username == buyerUsername);

            if (cartItem == null)
                return NotFound(new { message = "Cart item not found" });

            _dbContext.Carts.Remove(cartItem);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product removed from cart" });
        }

        // ✅ 4. إتمام الطلب (Checkout)
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var buyerUsername = User.Identity.Name;
            var buyer = await _dbContext.Users
                .Include(u => u.Carts)
                .ThenInclude(c => c.Product)
                .FirstOrDefaultAsync(u => u.Username == buyerUsername);

            if (buyer == null || buyer.Role != "Buyer" || !buyer.Carts.Any())
                return BadRequest(new { message = "Cart is empty or unauthorized" });

            var totalAmount = buyer.Carts.Sum(c => c.Quantity * c.Product.Price);

            var order = new Order
            {
                UserId = buyer.Id,
                TotalPrice = totalAmount,
                CreatedAt = DateTime.Now,
                Status = "Pending"
            };

            await _dbContext.Orders.AddAsync(order);
            _dbContext.Carts.RemoveRange(buyer.Carts);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Order placed successfully", totalAmount });
        }

        // ✅ 5. عرض الطلبات
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var buyerUsername = User.Identity.Name;
            var orders = await _dbContext.Orders
                .Where(o => o.User.Username == buyerUsername)
                .ToListAsync();

            return Ok(orders);
        }

        public class CartItemModel
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class WishlistItemModel
        {
            public int ProductId { get; set; }
        }
    }
}
