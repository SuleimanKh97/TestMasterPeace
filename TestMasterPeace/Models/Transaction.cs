using System;
using System.Collections.Generic;

namespace TestMasterPeace.Models;

public partial class Transaction
{
    public long Id { get; set; }

    public long? OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime? TransactionDate { get; set; }

    public virtual Order? Order { get; set; }
}
