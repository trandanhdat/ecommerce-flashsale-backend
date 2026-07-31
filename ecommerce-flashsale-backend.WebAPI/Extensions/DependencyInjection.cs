using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Services.Admin;
using FlashSale.Application.Services.Auth;
using FlashSale.Application.Services.UserProfile;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.Users;
using FlashSale.Infrastructure.Caching;
using FlashSale.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce_flashsale_backend.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services) { 
            // MediatR cho CQRS Query
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FlashSale.Application.Services.Auth.IAuthService).Assembly));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductAdminService, ProductAdminService>();
            services.AddScoped<IBannerService, BannerService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<FlashSale.Application.Services.Admin.IDashboardService, FlashSale.Infrastructure.Services.Admin.DashboardService>();
            
            // Events & UnitOfWork
            services.AddScoped<IEventPublisher, FlashSale.Infrastructure.Messaging.NoOpEventPublisher>();
            services.AddScoped<FlashSale.Domain.SeedWork.IUnitOfWork>(provider => provider.GetRequiredService<FlashSale.Infrastructure.Persistence.ApplicationDbContext>());
            services.AddScoped<IProductCatalogCacheWarmer, ProductCatalogCacheWarmer>();
            services.AddScoped<IProductCatalogCache, RedisProductCatalogCache>();
            services.AddScoped<IFlashSaleStockCache, RedisFlashSaleStockCache>();
            services.AddScoped<IDistributedLockService, RedisDistributedLockService>();

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<FlashSale.Application.Services.Cart.ICartService, FlashSale.Application.Services.Cart.CartService>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
            services.AddScoped<IBannerRepository, BannerRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<FlashSale.Domain.Reservations.IReservationRepository, ReservationRepository>();
            services.AddScoped<FlashSale.Domain.Orders.IOrderRepository, OrderRepository>();
            services.AddScoped<FlashSale.Domain.Payments.IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentGateway, FlashSale.Infrastructure.PaymentGateways.VnPaySandboxGateway>();

            // External Services (Email, Realtime)
            services.AddTransient<IEmailService, FlashSale.Infrastructure.Email.SmtpEmailService>();
            services.AddTransient<INotificationHub, FlashSale.Infrastructure.Realtime.SignalRNotificationHub>();

            return services;
        }
    }
}
