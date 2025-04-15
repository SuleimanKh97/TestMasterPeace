using System;

namespace TestMasterPeace.DTOs;

public class BlogPostSummaryDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string AuthorName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class BlogPostDetailDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string AuthorName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateBlogPostDTO
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class UpdateBlogPostDTO
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsPublished { get; set; }
}

public class AdminBlogPostListDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsPublished { get; set; }
    public string AuthorName { get; set; } = null!;
} 