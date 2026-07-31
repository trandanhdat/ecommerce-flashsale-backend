using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FlashSale.Domain.Users;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.Catalog.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            await context.Database.MigrateAsync();

            // Seed Roles
            var roles = new[] { UserRole.Admin.ToString(), UserRole.Customer.ToString() };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }

            // Seed Admin User
            const string adminEmail = "admin@flashsale.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User("admin", adminEmail, "System Administrator");
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                }
            }

            // Seed Categories
            var categories = await context.Categories.ToListAsync();
            if (categories.Count < 4)
            {
                var electronics = categories.FirstOrDefault(c => c.Slug == "electronics");
                if (electronics == null) { electronics = new Category("Electronics", "electronics", "Electronic devices and accessories"); context.Categories.Add(electronics); }

                var clothing = categories.FirstOrDefault(c => c.Slug == "clothing");
                if (clothing == null) { clothing = new Category("Clothing", "clothing", "Apparel and fashion items"); context.Categories.Add(clothing); }

                var gaming = categories.FirstOrDefault(c => c.Slug == "gaming");
                if (gaming == null) { gaming = new Category("Gaming", "gaming", "Gaming consoles and accessories"); context.Categories.Add(gaming); }

                var home = categories.FirstOrDefault(c => c.Slug == "home-appliances");
                if (home == null) { home = new Category("Home Appliances", "home-appliances", "Smart home devices"); context.Categories.Add(home); }
                
                await context.SaveChangesAsync();

                // Seed Products
                var productCount = await context.Products.CountAsync();
                if (productCount < 5)
                {
                    var product1 = new Product(electronics.Id, "ELEC-001", "iPhone 15 Pro Max", "Latest Apple smartphone", "https://example.com/iphone.jpg", new Money(29000000), 100);
                    var product2 = new Product(clothing.Id, "CLOT-001", "Áo thun Local Brand", "100% Cotton", "https://example.com/tshirt.jpg", new Money(350000), 500);
                    var product3 = new Product(gaming.Id, "GAME-001", "PlayStation 5", "Sony PS5 Console", "https://example.com/ps5.jpg", new Money(15000000), 50);
                    var product4 = new Product(gaming.Id, "GAME-002", "Bàn phím cơ Razer", "Razer BlackWidow", "https://example.com/razer.jpg", new Money(3000000), 200);
                    var product5 = new Product(home.Id, "HOME-001", "Robot hút bụi Xiaomi", "Xiaomi Vacuum Mop 2", "https://example.com/xiaomi.jpg", new Money(6000000), 30);

                    context.Products.AddRange(product1, product2, product3, product4, product5);
                    await context.SaveChangesAsync();
                }
            }

            // Seed FlashSale for Load Testing
            var hasActiveSale = await context.FlashSales.AnyAsync(f => f.Status == FlashSale.Domain.FlashSales.FlashSaleStatus.Active || f.Status == FlashSale.Domain.FlashSales.FlashSaleStatus.Upcoming);
            if (!hasActiveSale)
            {
                var targetProduct = await context.Products.FirstOrDefaultAsync();
                if (targetProduct == null)
                {
                    // Nếu chưa có Product nào (do category đã seed trước đó nên bị skip), tự tạo 1 cái
                    var cat = await context.Categories.FirstOrDefaultAsync();
                    if (cat == null)
                    {
                        cat = new Category("Test", "test", "Test");
                        context.Categories.Add(cat);
                        await context.SaveChangesAsync();
                    }
                    targetProduct = new Product(cat.Id, "TEST-001", "Test Product", "Test", "url", new Money(1000), 100);
                    context.Products.Add(targetProduct);
                    await context.SaveChangesAsync();
                }

                // Flash sale bắt đầu từ hôm nay và kết thúc sau 2 ngày
                var startTime = DateTime.UtcNow.AddMinutes(-5); // Lùi lại 5 phút để Hangfire Activate ngay lập tức
                var endTime = DateTime.UtcNow.AddDays(2);
                
                var flashSale = new FlashSale.Domain.FlashSales.FlashSale("Siêu Sale Load Test " + DateTime.Now.ToString("dd/MM HH:mm"), startTime, endTime);
                
                // Add FlashSaleItem (Product: targetProduct, Giá 20tr, Stock: 50, Max 5/User)
                var flashSaleItem = new FlashSale.Domain.FlashSales.FlashSaleItem(flashSale.Id, targetProduct.Id, new Money(20000000), 50, 5);
                
                context.FlashSales.Add(flashSale);
                context.Add(flashSaleItem);
                
                await context.SaveChangesAsync();
            }
        }
    }
}
