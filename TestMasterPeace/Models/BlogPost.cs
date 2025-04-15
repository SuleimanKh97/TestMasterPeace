using System;
using System.Collections.Generic;

namespace TestMasterPeace.Models;

public partial class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public long? AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsPublished { get; set; }

    public virtual User? Author { get; set; }
} 