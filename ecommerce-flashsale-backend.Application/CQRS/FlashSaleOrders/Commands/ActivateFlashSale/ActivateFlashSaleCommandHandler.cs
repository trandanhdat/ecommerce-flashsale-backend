using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.ActivateFlashSale
{
    public class ActivateFlashSaleCommandHandler : IRequestHandler<ActivateFlashSaleCommand>
    {
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IFlashSaleStockCache _flashSaleStockCache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ActivateFlashSaleCommandHandler> _logger;

        public ActivateFlashSaleCommandHandler(
            IFlashSaleRepository flashSaleRepository,
            IFlashSaleStockCache flashSaleStockCache,
            IUnitOfWork unitOfWork,
            ILogger<ActivateFlashSaleCommandHandler> logger)
        {
            _flashSaleRepository = flashSaleRepository;
            _flashSaleStockCache = flashSaleStockCache;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(ActivateFlashSaleCommand request, CancellationToken cancellationToken)
        {
            // a) Load FlashSale + toàn bộ FlashSaleItems liên quan
            var flashSale = await _flashSaleRepository.GetByIdWithItemsAsync(request.FlashSaleId, cancellationToken);
            if (flashSale == null) return;

            // b) Kiểm tra Status hiện tại = Upcoming và StartTime <= UtcNow (idempotent)
            if (flashSale.Status != FlashSaleStatus.Upcoming || flashSale.StartTime > DateTime.UtcNow)
            {
                _logger.LogInformation("Job ActivateFlashSale bỏ qua FlashSale {FlashSaleId} (Status: {Status}, StartTime: {StartTime}) vì đã active hoặc chưa tới giờ.", flashSale.Id, flashSale.Status, flashSale.StartTime);
                return;
            }

            // c) Với MỖI FlashSaleItem → gọi InitStockAsync
            foreach (var item in flashSale.Items)
            {
                await _flashSaleStockCache.InitStockAsync(item.Id, item.SaleStock, cancellationToken);
            }

            // d) Cập nhật FlashSale.Status = Active qua domain method
            flashSale.Activate();

            // e) SaveChanges
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã kích hoạt thành công FlashSale {FlashSaleId} và khởi tạo kho Redis thành công.", flashSale.Id);
            
            // f) Publish SignalR notification (NoOp tạm)
            // TODO: Bắn thông báo realtime ở Phase 8
        }
    }
}
