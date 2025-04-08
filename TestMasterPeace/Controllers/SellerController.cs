using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.DTOs.ProductsDTOs;
using TestMasterPeace.Models;
using TestMasterPeace.Services;

namespace TestMasterPeace.Controllers
{


    [Route("[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {
        private readonly MasterPeiceContext _dbContext;

        public SellerController(MasterPeiceContext dbContext)
        {
            _dbContext = dbContext;
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

       
    }
}
