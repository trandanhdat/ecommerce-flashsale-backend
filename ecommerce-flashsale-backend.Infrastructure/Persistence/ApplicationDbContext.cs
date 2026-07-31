using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FlashSale.Domain.Users;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.Notifications;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Payments;
using FlashSale.Domain.Reservations;
using System.Reflection;

using FlashSale.Domain.SeedWork;

namespace FlashSale.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IUnitOfWork
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Banner> Banners => Set<Banner>();

        public DbSet<FlashSale.Domain.FlashSales.FlashSale> FlashSales => Set<FlashSale.Domain.FlashSales.FlashSale>();
        public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<CartItem> CartItems => Set<CartItem>();

        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // This applies Identity configurations

            // Apply all configurations defined in this assembly
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
