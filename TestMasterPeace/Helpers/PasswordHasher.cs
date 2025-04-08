namespace TestMasterPeace.Helpers;

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

public static class PasswordHasher
{
    private static string _securityKey;

    // You can initialize this class with the IConfiguration object in Startup/Program to get the key
    static PasswordHasher()
    {
        // Retrieve the security key from appsettings.json
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        _securityKey = builder["JwtSettings:SecretKey"];
    }

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_securityKey)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);  // Return the hashed password as Base64 string
        }
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var hashedPasswordAttempt = HashPassword(password);
        return hashedPasswordAttempt == hashedPassword;  // Compare the computed hash to the stored hash
    }
}