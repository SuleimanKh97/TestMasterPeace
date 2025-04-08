using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.DTOs.ProductsDTOs;
using TestMasterPeace.Models;

namespace TestMasterPeace.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CategoryController(MasterPeiceContext context) : ControllerBase
    {

        [HttpGet("ListCategory")]
        public async Task<IActionResult> GetCategory()
        {
            return Ok(await context.Categories.ToListAsync());
        }


        [HttpPost]
        public async Task<IActionResult> AddCategory(CreateCategoryRequest createCategory)
        {
            await context.Categories.AddAsync(new Models.Category
            {
                Name = createCategory.Name,
                Description = createCategory.Description
            });
            return Ok(await context.SaveChangesAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return NotFound(new { message = "noCategoryFounded" });
            return Ok(category);

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // Find the product in the database
            var category = await context.Categories.FirstOrDefaultAsync(p => p.Id == id);

            if (category == null)
            {
                return NotFound(new { message = "category not found" });
            }

            // Remove the product from the database
            context.Categories.Remove(category);
            await context.SaveChangesAsync();

            return Ok(new { message = "category deleted successfully" });
        }
    }
}
