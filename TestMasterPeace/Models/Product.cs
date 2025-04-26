using System;
using System.Collections.Generic;

namespace TestMasterPeace.Models;

public partial class Product
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public long? CategoryId { get; set; }

    public long? SellerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Img { get; set; }

    public bool IsSold { get; set; } = false;

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? Seller { get; set; }
}
