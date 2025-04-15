using System;
using System.Collections.Generic;

namespace TestMasterPeace.Models;

public partial class User
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? ProfileImg { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<ContactU> ContactUs { get; set; } = new List<ContactU>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    
    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
}
