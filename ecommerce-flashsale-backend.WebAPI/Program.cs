using System;
using System.Linq;
using System.Text;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Services.Auth;
using FlashSale.Domain.Users;
using FlashSale.Infrastructure.Identity;
using FlashSale.Infrastructure.Persistence;
using FlashSale.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ecommerce_flashsale_backend.Middlewares;
using ecommerce_flashsale_backend.Extensions;
using Hangfire;
using Hangfire.MemoryStorage;
using FlashSale.WebAPI.BackgroundJobs;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Serilog;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ─── SERILOG ─────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", b => b
        .WithOrigins("http://localhost:3000", "http://127.0.0.1:5500", "http://localhost:5500")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// ─── EXTENSION CONFIGURATIONS ────────────────────────────────────────────────
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddIdentityAndJwt(builder.Configuration);
builder.Services.AddRateLimitingConfiguration();
builder.Services.AddSwaggerConfiguration();

// ─── DEPENDENCY INJECTION ────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(FlashSale.Application.Services.Admin.Mappings.CategoryMappingProfile).Assembly));
builder.Services.AddValidatorsFromAssembly(AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ecommerce-flashsale-backend.Application"));

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
var redisOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AbortOnConnectFail = false; // Ngăn chặn crash app khi Redis bị sập, thay vào đó sẽ tự động Retry

builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp => 
{
    var multiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(redisOptions);
    
    // Thêm các sự kiện lắng nghe để báo ra Terminal
    multiplexer.ConnectionRestored += (sender, args) =>
    {
        Serilog.Log.Information("🟢 [Redis] Đã kết nối lại thành công tới Redis server: {EndPoint}", args.EndPoint);
    };

    multiplexer.ConnectionFailed += (sender, args) =>
    {
        Serilog.Log.Warning("🔴 [Redis] Mất kết nối tới Redis server: {EndPoint}. Đang thử kết nối lại...", args.EndPoint);
    };

    return multiplexer;
});

builder.Services.AddApplicationServices();

// ─── HANGFIRE ────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-commerce-flashSale API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() } 
});

// Configure Recurring Jobs
RecurringJob.AddOrUpdate<FlashSaleJobRunner>("scan-activate-flashsale", runner => runner.ScanAndActivatePendingFlashSalesAsync(), "*/1 * * * *");
RecurringJob.AddOrUpdate<FlashSaleJobRunner>("scan-end-flashsale", runner => runner.ScanAndEndFlashSalesAsync(), "*/1 * * * *");
RecurringJob.AddOrUpdate<FlashSaleJobRunner>("expire-reservations", runner => runner.ExpireReservationsAsync(), "*/1 * * * *");
RecurringJob.AddOrUpdate<FlashSaleJobRunner>("sync-flashsale-stock", runner => runner.SyncFlashSaleStockAsync(), "*/1 * * * *");

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.MapControllers();
app.MapHub<FlashSale.Infrastructure.Realtime.FlashSaleHub>("/hubs/flashsale");
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();
