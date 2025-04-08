using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.DTOs.ProductsDTOs;
using TestMasterPeace.Models;

namespace TestMasterPeace.Controllers;

[Route("[controller]")]
[ApiController]
public class ProductController(MasterPeiceContext context) : ControllerBase
{
    [HttpGet("listProduct")]
    public async Task<IActionResult> GetProducts()
    {
        return Ok(await context.Products.ToListAsync());
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
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
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

        await context.Products.AddAsync(product);
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
