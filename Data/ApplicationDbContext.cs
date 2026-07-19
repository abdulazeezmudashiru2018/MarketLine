using Microsoft.EntityFrameworkCore;
using MarketLine.Models;
using System;

namespace MarketLine.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sale> Sales { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<SaleInvoice> SaleInvoices { get; set; }
        public DbSet<SaleInvoiceItem> SaleInvoiceItems { get; set; }
        public DbSet<LandingMedia> LandingMediaItems { get; set; }
        public DbSet<CustomerOrder> CustomerOrders { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicit precision/scale for money values (avoids the
            // "No store type was specified for decimal property" warning)
            modelBuilder.Entity<Sale>()
                .Property(s => s.Amount)
              .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
              .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<SaleInvoice>()
                .Property(s => s.TotalAmount)
              .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<SaleInvoiceItem>()
                .Property(i => i.Quantity)
               .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<SaleInvoiceItem>()
                .Property(i => i.UnitPrice)
               .HasColumnType("numeric(18,2)");
            modelBuilder.Entity<SaleInvoiceItem>()
                .Property(i => i.TotalPrice)
               .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SaleInvoiceItem>()
                .HasOne(i => i.SaleInvoice)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SaleInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            var today = DateTime.SpecifyKind(new DateTime(2026, 7, 15), DateTimeKind.Utc);


            modelBuilder.Entity<Sale>().HasData(
                new Sale { Id = 1, CustomerName = "John Smith",     Amount = 3200m,  SaleDate = today },
                new Sale { Id = 2, CustomerName = "Sara Williams",  Amount = 6150m,  SaleDate = today },
                new Sale { Id = 3, CustomerName = "Michael Brown",  Amount = 2980m,  SaleDate = today.AddDays(-1) },
                new Sale { Id = 4, CustomerName = "Emily Johnson",  Amount = 4750m,  SaleDate = today.AddDays(-1) },
                new Sale { Id = 5, CustomerName = "David Lee",      Amount = 15230m, SaleDate = today.AddDays(-2) },
                new Sale { Id = 6, CustomerName = "Grace Adams",    Amount = 1250m,  SaleDate = today.AddDays(-3) }
            );

            // Add this inside protected override void OnModelCreating(ModelBuilder modelBuilder)
            modelBuilder.Entity<CustomerOrder>()
                .Property(o => o.Total)
              .HasColumnType("numeric(18,2)");
        }



    }

}

