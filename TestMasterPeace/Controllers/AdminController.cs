using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.Models;
using TestMasterPeace.Helpers;
using Microsoft.AspNetCore.Authorization;


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

        // ✅ استرجاع جميع الطلبات
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _dbContext.Orders.ToListAsync();
            return Ok(orders);
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
