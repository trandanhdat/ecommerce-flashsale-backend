using System;
using System.Linq;
using System.Threading.Tasks;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.ActivateFlashSale;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.EndFlashSale;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.ExpireReservations;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.SyncFlashSaleStockToDb;
using FlashSale.Domain.FlashSales;
using FlashSale.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlashSale.WebAPI.BackgroundJobs
{
    // Lớp trung gian để Hangfire có thể gọi MediatR (vì Hangfire cần truyền delegate gọi public methods cụ thể)
    public class FlashSaleJobRunner
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly ILogger<FlashSaleJobRunner> _logger;

        public FlashSaleJobRunner(ApplicationDbContext dbContext, IMediator mediator, ILogger<FlashSaleJobRunner> logger)
        {
            _dbContext = dbContext;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task ScanAndActivatePendingFlashSalesAsync()
        {
            var now = DateTime.UtcNow;
            var upcomingSales = await _dbContext.FlashSales
                .Where(f => f.Status == FlashSaleStatus.Upcoming && f.StartTime <= now)
                .Select(f => f.Id)
                .ToListAsync();

            foreach (var id in upcomingSales)
            {
                _logger.LogInformation("Hangfire: Đang gửi lệnh kích hoạt FlashSale {Id}", id);
                await _mediator.Send(new ActivateFlashSaleCommand(id));
            }
        }

        public async Task ScanAndEndFlashSalesAsync()
        {
            var now = DateTime.UtcNow;
            var activeSalesToEnd = await _dbContext.FlashSales
                .Where(f => f.Status == FlashSaleStatus.Active && f.EndTime <= now)
                .Select(f => f.Id)
                .ToListAsync();

            foreach (var id in activeSalesToEnd)
            {
                _logger.LogInformation("Hangfire: Đang gửi lệnh kết thúc FlashSale {Id}", id);
                await _mediator.Send(new EndFlashSaleCommand(id));
            }
        }

        public async Task ExpireReservationsAsync()
        {
            _logger.LogInformation("Hangfire: Đang quét hết hạn giữ chỗ (ExpireReservations)...");
            await _mediator.Send(new ExpireReservationsCommand());
        }

        public async Task SyncFlashSaleStockAsync()
        {
            _logger.LogInformation("Hangfire: Đang đồng bộ số liệu bán (SyncFlashSaleStock)...");
            await _mediator.Send(new SyncFlashSaleStockToDbCommand());
        }
    }
}
