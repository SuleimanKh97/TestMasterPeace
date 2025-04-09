using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.DTOs.ProductsDTOs;
// using TestMasterPeace.DTOs.CartDTOs; // Removed potentially unused using
using TestMasterPeace.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims; 
// using TestMasterPeace.DTOs.ProductDetail; // DTO is defined below, no need for using here

namespace TestMasterPeace.Controllers; // Assuming file-scoped namespace

[Route("[controller]")]
[ApiController]
public class ProductController(MasterPeiceContext context) : ControllerBase
{
    [HttpGet("listProduct")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await context.Products
            .Include(p => p.Seller)
            .Select(p => new ShopProductDTO
            {
                Id = (int)p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Img = p.Img,
                SellerName = p.Seller != null ? p.Seller.Username : "Unknown Seller" // Corrected to Username
            })
            .ToListAsync();
        return Ok(products);
    }
    [HttpPost("NewProduct")]
    public async Task<IActionResult> PostProduct(CreateProductRequest newProduct)
    {

        var newproduct = new Product
        {
            Name = newProduct.Name,
            Description = newProduct.Description,
            Price = newProduct.Price,
            CategoryId = newProduct.CategoryId,
            Img = newProduct.Img,
            CreatedAt = DateTime.Now
        };

        await context.Products.AddAsync(newproduct);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProductById), new { id = newproduct.Id}, newproduct);
    }

    [HttpGet("GetProductBy{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Where(p => p.Id == id)
            .Select(p => new ProductDetailDTO // Use DTO defined below
            {
                Id = (int)p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Img = p.Img,
                CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                SellerName = p.Seller != null ? p.Seller.Username : "Unknown Seller", // Corrected to Username
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }
        return Ok(product);
    }

    [HttpPut("EditProductBy{id}")]
    public async Task<IActionResult> EditProductId(int id , CreateProductRequest updateProduct)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if(product == null)
            return NotFound();
        product.Name = updateProduct.Name;
        product.Description = updateProduct.Description;
        product.Price = updateProduct.Price;
        product.CategoryId = updateProduct.CategoryId;
        product.CreatedAt = DateTime.Now;
        // Consider using Update method instead of AddAsync for existing entities
        context.Products.Update(product);
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("deleteProductby{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        // Find the product in the database
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        // Remove the product from the database
        context.Products.Remove(product);
        await context.SaveChangesAsync();

        return Ok(new { message = "Product deleted successfully" });
    }
}

// --- DTO Definitions placed directly in the file (no separate namespace block) ---

// DTO for Shop page product list (Ensure this matches the usage in GetProducts)
public class ShopProductDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Img { get; set; }
    public string SellerName { get; set; }
}

// DTO for Product Detail Page
public class ProductDetailDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Img { get; set; }
    public string CategoryName { get; set; }
    public string SellerName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

// Removed the empty CartDTOs namespace block
