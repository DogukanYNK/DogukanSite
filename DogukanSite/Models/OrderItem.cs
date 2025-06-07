﻿using System.ComponentModel.DataAnnotations.Schema;

namespace DogukanSite.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtTimeOfPurchase { get; set; } // Satın alındığı andaki fiyat

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}