using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TestMasterPeace.Models;

public partial class Order
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    // --- Shipping Information --- 
    [Required]
    [StringLength(15)]
    public string ShippingPhoneNumber { get; set; }

    [Required]
    [StringLength(100)]
    public string ShippingAddressLine1 { get; set; }

    [StringLength(100)]
    public string? ShippingAddressLine2 { get; set; }

    [Required]
    [StringLength(50)]
    public string ShippingCity { get; set; }

    // Add other fields like PostalCode, Country etc. if needed
    // --------------------------

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual User? User { get; set; }
}
