using System.Collections.Generic;
using System;

namespace TestMasterPeace.DTOs.AdminDTOs
{
    // DTO for individual items within an admin order view
    public class AdminOrderItemDTO
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public long? SellerId { get; set; } // ID of the seller for this item
        public string SellerUsername { get; set; } // Username of the seller
    }

    // DTO for the detailed order view for an admin
    public class AdminOrderDetailDTO
    {
        public long OrderId { get; set; }
        public long BuyerUserId { get; set; } // ID of the buyer
        public string BuyerUsername { get; set; } // Username of the buyer
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // Current status
        public string PaymentMethod { get; set; } // From Transaction
        public DateTime? TransactionDate { get; set; } // From Transaction
        // Add other dates like ShippedDate, DeliveredDate if available in your Order/StatusHistory model

        public List<AdminOrderItemDTO> OrderItems { get; set; }

        public AdminOrderDetailDTO()
        {
            OrderItems = new List<AdminOrderItemDTO>();
        }
    }
} 