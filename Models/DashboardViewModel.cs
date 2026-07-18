
using System;
using System.Collections.Generic;

namespace MarketLine.Models
{
    public class DashboardViewModel
    {
        public decimal HighestSale { get; set; }
        public decimal LowestSale { get; set; }
        public decimal TotalSales { get; set; }
        public List<Sale> RecentSales { get; set; } = new();
        public int NotificationCount { get; set; }
        public List<decimal> TrendPoints { get; set; } = new();

        // ✨ Add this property for the Top 12 Customers
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
    }

    public class TopCustomerDto
    {
        public int Rank { get; set; }

        public string Name { get; set; } = string.Empty;

        // Total from shop sales + online orders
        public decimal TotalSpent { get; set; }

        public int PurchaseCount { get; set; }

        public int ShopPurchaseCount { get; set; }
        public decimal ShopPurchaseTotal { get; set; }

        public int OnlineOrderCount { get; set; }
        public decimal OnlineOrderTotal { get; set; }

        public DateTime LastPurchaseDate { get; set; }
        public string LatestPurchaseSource { get; set; } = string.Empty;

        // Available only if this customer has made an online order
        public string Email { get; set; } = "Not available";
        public string ShippingAddress { get; set; } = "Not available";
        public string City { get; set; } = "Not available";
        public string PaymentMethod { get; set; } = "Not available";
    }
}