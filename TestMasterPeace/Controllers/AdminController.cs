using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.Models;
using TestMasterPeace.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using TestMasterPeace.DTOs.AdminDTOs;
using System.Collections.Generic;

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly MasterPeiceContext _dbContext;

        public AdminController(MasterPeiceContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ✅ 1. استرجاع بيانات لوحة تحكم المشرف
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalUsers = await _dbContext.Users.CountAsync();
            var totalProducts = await _dbContext.Products.CountAsync();
            var totalOrders = await _dbContext.Orders.CountAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders
            });
        }

        //returns users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _dbContext.Users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.CreatedAt
            }).ToListAsync();

            return Ok(users);
        }

        // add new User
        [HttpPost("users")]
        public async Task<IActionResult> AddUser([FromBody] RegisterModel newUser)
        {
            if (_dbContext.Users.Any(u => u.Username == newUser.Username || u.Email == newUser.Email))
                return BadRequest(new { message = "Username or email already exists" });

            var user = new User
            {
                Username = newUser.Username,
                Email = newUser.Email,
                Password = PasswordHasher.HashPassword(newUser.Password),
                Role = newUser.Role, // Buyer, Seller, Admin, etc.
                CreatedAt = DateTime.Now
            };

          

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "User added successfully" });
        }

        //ban Users
        [HttpPut("users/{id}/ban")]
        public async Task<IActionResult> BanUser(long id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.Role = user.Role == "Banned" ? "Buyer" : "Banned"; // تبديل الحالة
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = $"User {(user.Role == "Banned" ? "banned" : "unbanned")} successfully" });
        }


        //delete User
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            // Prevent deleting the admin themselves? Or handled in frontend?
            // Add logic here if needed, e.g., check if user.Role == "Admin"

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }

        // Update User Details
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserModel updatedUserData)
        {
            var userToUpdate = await _dbContext.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound(new { message = "User not found" });

            // Check for potential conflicts with other users
            if (await _dbContext.Users.AnyAsync(u => u.Id != id && (u.Username == updatedUserData.Username || u.Email == updatedUserData.Email)))
            {
                return BadRequest(new { message = "Username or Email is already taken by another user." });
            }

            // Update user properties
            userToUpdate.Username = updatedUserData.Username;
            userToUpdate.Email = updatedUserData.Email;
            userToUpdate.Role = updatedUserData.Role; // Make sure Role is validated if needed
            // Do NOT update password here. Use a separate endpoint for password changes.

            try
            {
                await _dbContext.SaveChangesAsync();
                // Return the updated user data (excluding sensitive info like password)
                 var updatedUserResponse = new
                {
                    userToUpdate.Id,
                    userToUpdate.Username,
                    userToUpdate.Email,
                    userToUpdate.Role,
                    userToUpdate.CreatedAt
                };
                return Ok(new { message = "User updated successfully", user = updatedUserResponse });
            }
            catch (DbUpdateConcurrencyException)
            {
                 // Handle concurrency issues if necessary
                return Conflict(new { message = "The user data was modified by another process. Please reload and try again." });
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                return StatusCode(500, new { message = "An error occurred while updating the user." });
            }
        }

        // ✅ استرجاع جميع المنتجات للمراجعة
        [HttpGet("products")]
        public async Task<IActionResult> GetProductsForReview()
        {
            var products = await _dbContext.Products.ToListAsync();
            return Ok(products);
        }

        // ✅ حذف منتج مخالف
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Product not found" });

            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }

        /* Comment out the ambiguous simpler endpoint 
        // ✅ استرجاع جميع الطلبات (Simple version - Ambiguous)
        [HttpGet("orders")] // This route can cause ambiguity with [HttpGet("Orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _dbContext.Orders.ToListAsync();
            return Ok(orders);
        }
        */

        // GET: /Admin/Orders (Keep this detailed version)
        [HttpGet("Orders")]
        public async Task<ActionResult<IEnumerable<AdminOrderDetailDTO>>> GetAllOrders()
        {
            var orders = await _dbContext.Orders
                .Include(o => o.User) // Include Buyer info
                .Include(o => o.Transactions) // Include Transactions to get PaymentMethod
                .Include(o => o.OrderItems) // Include OrderItems
                    .ThenInclude(oi => oi.Product) // Include Product for each OrderItem
                        .ThenInclude(p => p.Seller) // Include Seller for each Product
                .OrderByDescending(o => o.CreatedAt) // Show newest first
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                return Ok(new List<AdminOrderDetailDTO>()); // Return empty list if no orders
            }

            // Manual Mapping from Order entities to AdminOrderDetailDTOs
            var orderDTOs = orders.Select(order => new AdminOrderDetailDTO
            {
                OrderId = order.Id,
                BuyerUserId = order.User?.Id ?? 0, // Handle potential null user
                BuyerUsername = order.User?.Username ?? "Unknown", // Handle potential null user
                OrderDate = order.CreatedAt ?? DateTime.MinValue, // Handle potential null date
                TotalAmount = order.TotalPrice,
                Status = order.Status,
                PaymentMethod = order.Transactions.FirstOrDefault()?.PaymentMethod ?? "N/A", // Get from first transaction
                TransactionDate = order.Transactions.FirstOrDefault()?.TransactionDate, // Get from first transaction
                OrderItems = order.OrderItems.Select(oi => new AdminOrderItemDTO
                {
                    ProductId = (long)oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    ImageUrl = oi.Product?.Img ?? "", // Handle potential null product/image
                    SellerId = oi.Product?.SellerId,
                    SellerUsername = oi.Product?.Seller?.Username ?? "Unknown Seller"
                }).ToList()
            }).ToList();

            return Ok(orderDTOs);
        }
    }
}

// Define the model for the update request
public class UpdateUserModel
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; } // Consider validation or enum
}
