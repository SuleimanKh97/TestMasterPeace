using System;
using System.Collections.Generic;

namespace TestMasterPeace.Models;

public partial class ContactU
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
