using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMasterPeace.DTOs;
using TestMasterPeace.Models;
using System.Linq;

namespace TestMasterPeace.Controllers;

[Route("[controller]")]
[ApiController]
public class BlogController(MasterPeiceContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBlogPosts()
    {
        var blogPosts = await context.BlogPosts
            .Where(bp => bp.IsPublished)
            .Include(bp => bp.Author)
            .OrderByDescending(bp => bp.CreatedAt)
            .Select(bp => new BlogPostSummaryDTO
            {
                Id = bp.Id,
                Title = bp.Title,
                Summary = bp.Content.Length > 200 ? bp.Content.Substring(0, 200) + "..." : bp.Content,
                ImageUrl = bp.ImageUrl,
                AuthorName = bp.Author != null ? bp.Author.Username : "Anonymous",
                CreatedAt = bp.CreatedAt
            })
            .ToListAsync();

        return Ok(blogPosts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBlogPostById(int id)
    {
        var blogPost = await context.BlogPosts
            .Include(bp => bp.Author)
            .Where(bp => bp.Id == id && bp.IsPublished)
            .Select(bp => new BlogPostDetailDTO
            {
                Id = bp.Id,
                Title = bp.Title,
                Content = bp.Content,
                ImageUrl = bp.ImageUrl,
                AuthorName = bp.Author != null ? bp.Author.Username : "Anonymous",
                CreatedAt = bp.CreatedAt,
                UpdatedAt = bp.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (blogPost == null)
        {
            return NotFound(new { message = "Blog post not found" });
        }

        return Ok(blogPost);
    }

    [HttpGet("latest/{count}")]
    public async Task<IActionResult> GetLatestBlogPosts(int count = 3)
    {
        var blogPosts = await context.BlogPosts
            .Where(bp => bp.IsPublished)
            .Include(bp => bp.Author)
            .OrderByDescending(bp => bp.CreatedAt)
            .Take(count)
            .Select(bp => new BlogPostSummaryDTO
            {
                Id = bp.Id,
                Title = bp.Title,
                Summary = bp.Content.Length > 200 ? bp.Content.Substring(0, 200) + "..." : bp.Content,
                ImageUrl = bp.ImageUrl,
                AuthorName = bp.Author != null ? bp.Author.Username : "Anonymous",
                CreatedAt = bp.CreatedAt
            })
            .ToListAsync();

        return Ok(blogPosts);
    }
} 