using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.Helpers;
using TestMasterPeace.Models;
using TestMasterPeace.Services;

namespace TestMasterPeace.Controllers;

[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly MasterPeiceContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(MasterPeiceContext dbContext, JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    // ✅ تسجيل الدخول
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel login)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == login.Username);

        if (user == null || !PasswordHasher.VerifyPassword(login.Password, user.Password))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // ✅ إضافة الدور إلى التوكن
        var token = _jwtTokenService.GenerateJwtToken(user);

        return Ok(new { token, role = user.Role, username = user.Username, userId = user.Id });
    }

    // ✅ تسجيل مستخدم جديد
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel register)
    {
        // 🛑 التحقق مما إذا كان اسم المستخدم أو البريد الإلكتروني مستخدمًا بالفعل
        if (await _dbContext.Users.AnyAsync(u => u.Username == register.Username || u.Email == register.Email))
        {
            return BadRequest(new { message = "Username or Email already exists" });
        }

        var newUser = new User
        {
            Username = register.Username,
            Email = register.Email,
            Password = PasswordHasher.HashPassword(register.Password),
            Role = register.Role, // "Buyer" or "Seller"
            CreatedAt = DateTime.Now
        };

        await _dbContext.Users.AddAsync(newUser);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "User registered successfully" });
    }

    // ✅ الحصول على معلومات المستخدم الحالي
    [HttpGet("currentUser")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userName = User.Identity.Name;
        var user = await _dbContext.Users
            .Where(u => u.Username == userName)
            .Select(u => new { u.Username, u.Role, u.Email, u.CreatedAt, u.Orders })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound(new { message = "User not found" });

        return Ok(user);
    }

    // --- إضافة نقطة نهاية لتحديث الملف الشخصي ---
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
    {
        var currentUsername = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUsername))
        {
            return Unauthorized(new { message = "Cannot identify current user." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        bool changesMade = false;
        // Update username logic
        if (!string.IsNullOrEmpty(model.NewUsername) && model.NewUsername != user.Username)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Username == model.NewUsername))
            {
                return BadRequest(new { message = "New username is already taken." });
            }
            user.Username = model.NewUsername;
            changesMade = true;
        }
        // Update email logic
        if (!string.IsNullOrEmpty(model.NewEmail) && model.NewEmail != user.Email)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email == model.NewEmail))
            {
                return BadRequest(new { message = "New email is already taken." });
            }
            user.Email = model.NewEmail;
            changesMade = true;
        }

        // Update profile image logic
        if (!string.IsNullOrEmpty(model.ProfileImage) && model.ProfileImage != user.ProfileImg)
        {
            user.ProfileImg = model.ProfileImage;
            changesMade = true;
        }

        if (!changesMade)
        {
            // No actual data changes, return success without new token
            return Ok(new { message = "No changes detected in profile data.", updatedUser = new { user.Username, user.Email, user.Role, profileImage = user.ProfileImg } });
        }

        try
        {
            await _dbContext.SaveChangesAsync();

            // *** Regenerate JWT Token with updated user info ***
            var newToken = _jwtTokenService.GenerateJwtToken(user); 
            
            // *** Return the NEW token in the response ***
            return Ok(new {
                message = "Profile updated successfully.",
                newToken = newToken, // Ensure this is included
                updatedUser = new { user.Username, user.Email, user.Role, profileImage = user.ProfileImg }
            });
        }
        catch (DbUpdateException ex)
        {
            // Log the actual exception ex
            Console.WriteLine($"DB Update Error: {ex.Message}"); 
            return StatusCode(500, new { message = "An error occurred while updating the profile." });
        }
        catch (Exception ex)
        { 
            // Catch any other unexpected errors during token generation or saving
             Console.WriteLine($"Generic Error in UpdateProfile: {ex.Message}");
             return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }
    // ------------------------------------------

    // --- إضافة نقطة نهاية لتغيير كلمة المرور ---
    [HttpPut("change-password")]
    [Authorize] // User must be logged in
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        var currentUsername = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUsername)) { return Unauthorized(new { message = "User not authenticated." }); }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (user == null) { return NotFound(new { message = "User not found." }); }

        // التحقق من كلمة المرور الحالية
        if (!PasswordHasher.VerifyPassword(model.CurrentPassword, user.Password))
        {
            return BadRequest(new { message = "كلمة المرور الحالية غير صحيحة." });
        }

        // التحقق من أن كلمة المرور الجديدة ليست فارغة (يمكن إضافة تعقيد هنا)
        if (string.IsNullOrEmpty(model.NewPassword))
        {
            return BadRequest(new { message = "كلمة المرور الجديدة لا يمكن أن تكون فارغة." });
        }

        // تشفير وتحديث كلمة المرور الجديدة
        user.Password = PasswordHasher.HashPassword(model.NewPassword);

        try
        {
            await _dbContext.SaveChangesAsync();
            // لا داعي لإرجاع توكن جديد هنا بالضرورة، لكن يمكن إرسال رسالة نجاح
            return Ok(new { message = "تم تغيير كلمة المرور بنجاح." });
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"DB Update Error (ChangePassword): {ex.Message}");
            return StatusCode(500, new { message = "حدث خطأ أثناء تحديث كلمة المرور." });
        }
        catch (Exception ex)
        { 
             Console.WriteLine($"Generic Error in ChangePassword: {ex.Message}");
             return StatusCode(500, new { message = "حدث خطأ غير متوقع." });
        }
    }
    // ------------------------------------------
}

// ✅ نماذج الطلبات (Request Models)
public class LoginModel
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class RegisterModel
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Role { get; set; }
}

// --- Model for the update request ---
public class UpdateProfileModel
{
    // Add fields you want to allow updating
    public string? NewUsername { get; set; } // Nullable in case only other fields are updated
    public string? NewEmail { get; set; }    // Add Email field
    public string? ProfileImage { get; set; } // Add ProfileImage field for URL
    // Add password change fields separately for security
}

// --- Model for the change password request ---
public class ChangePasswordModel
{
    [System.ComponentModel.DataAnnotations.Required]
    public string CurrentPassword { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    // Add complexity validation attributes if desired (MinLength, Regex)
    public string NewPassword { get; set; }
}
