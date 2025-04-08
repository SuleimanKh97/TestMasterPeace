using System;
using System.Collections.Generic;

namespace TestMasterPeace.Models;

public partial class Cart
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public long? ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? User { get; set; }
}
