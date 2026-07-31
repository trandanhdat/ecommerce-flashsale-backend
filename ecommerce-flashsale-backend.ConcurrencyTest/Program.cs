using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Enums;
using FlashSale.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ecommerce_flashsale_backend.ConcurrencyTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== BẮT ĐẦU TEST CHỐNG OVERSELL VỚI LUA SCRIPT ===");

            // 1. Setup DI
            var services = new ServiceCollection();
            services.AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            
            // Thay "localhost:6379" bằng connection string thật nếu chạy trên server
            services.AddSingleton<IConnectionMultiplexer>(sp => 
                ConnectionMultiplexer.Connect("localhost:6379"));
            
            services.AddScoped<FlashSale.Application.Common.Interfaces.IFlashSaleStockCache, RedisFlashSaleStockCache>();

            var serviceProvider = services.BuildServiceProvider();
            var stockCache = serviceProvider.GetRequiredService<FlashSale.Application.Common.Interfaces.IFlashSaleStockCache>();

            var flashSaleItemId = Guid.NewGuid();
            int initialStock = 15;
            int totalConcurrentTasks = 20;

            // 2. Dọn dẹp & Init Stock
            Console.WriteLine($"\n[1] Khởi tạo tồn kho: {initialStock} sản phẩm (Item ID: {flashSaleItemId})");
            await stockCache.InitStockAsync(flashSaleItemId, initialStock);
            
            // 3. Test song song
            Console.WriteLine($"\n[2] Bắt đầu giả lập {totalConcurrentTasks} request tranh mua cùng lúc...");
            
            int successCount = 0;
            int insufficientCount = 0;
            int errorCount = 0;

            // Khởi tạo các Task
            var tasks = Enumerable.Range(1, totalConcurrentTasks).Select(async i =>
            {
                // Delay ngẫu nhiên một chút xíu để ép các luồng chạy thật sát nhau
                await Task.Delay(Random.Shared.Next(1, 10));
                
                try 
                {
                    var result = await stockCache.TryDecrementStockAsync(flashSaleItemId, 1);
                    if (result == StockDecrementResult.Success)
                        Interlocked.Increment(ref successCount);
                    else if (result == StockDecrementResult.InsufficientStock)
                        Interlocked.Increment(ref insufficientCount);
                    else
                        Interlocked.Increment(ref errorCount);
                }
                catch(Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                }
            }).ToList();

            var sw = Stopwatch.StartNew();
            await Task.WhenAll(tasks);
            sw.Stop();

            // 4. In kết quả
            Console.WriteLine($"\n[3] KẾT QUẢ SAU KHI TRANH MUA (Hoàn thành trong {sw.ElapsedMilliseconds}ms):");
            Console.WriteLine($"- Số người mua thành công (Success): {successCount}");
            Console.WriteLine($"- Số người bị từ chối (InsufficientStock): {insufficientCount}");
            Console.WriteLine($"- Lỗi khác (Errors): {errorCount}");
            
            // Kiểm tra stock thực tế còn lại trong Redis
            var finalStock = await stockCache.GetCurrentStockAsync(flashSaleItemId);
            Console.WriteLine($"- Tồn kho còn lại trên Redis: {finalStock}");

            // Đánh giá
            Console.WriteLine("\n[ĐÁNH GIÁ]:");
            if (successCount == initialStock && insufficientCount == (totalConcurrentTasks - initialStock) && finalStock == 0)
            {
                Console.WriteLine("✅ THÀNH CÔNG RỰC RỠ: Không có bất kỳ hiện tượng OVERSELL nào xảy ra!");
                Console.WriteLine("Cơ chế Lua Script đã block hoàn toàn các request thừa.");
            }
            else
            {
                Console.WriteLine("❌ CÓ LỖI XẢY RA: Có hiện tượng Oversell hoặc sai lệch dữ liệu!");
            }
        }
    }
}
