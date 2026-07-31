using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.UpdateFlashSaleStock
{
    public class UpdateFlashSaleStockCommandHandler : IRequestHandler<UpdateFlashSaleStockCommand, bool>
    {
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IFlashSaleStockCache _flashSaleStockCache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateFlashSaleStockCommandHandler> _logger;

        public UpdateFlashSaleStockCommandHandler(
            IFlashSaleRepository flashSaleRepository,
            IFlashSaleStockCache flashSaleStockCache,
            IUnitOfWork unitOfWork,
            ILogger<UpdateFlashSaleStockCommandHandler> logger)
        {
            _flashSaleRepository = flashSaleRepository;
            _flashSaleStockCache = flashSaleStockCache;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdateFlashSaleStockCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy FlashSaleItem từ DB
            var item = await _flashSaleRepository.GetActiveItemByIdAsync(request.FlashSaleItemId, cancellationToken);
            if (item == null)
            {
                _logger.LogWarning("Không tìm thấy FlashSaleItem hoặc chưa Active: {ItemId}", request.FlashSaleItemId);
                return false;
            }

            // 2. Tăng số lượng trong SQL (Bút toán sổ sách)
            item.AddStock(request.QuantityToAdd);
            
            // 3. Tăng số lượng trong Redis (Bơm hàng vào kho)
            await _flashSaleStockCache.IncrementStockAsync(request.FlashSaleItemId, request.QuantityToAdd, cancellationToken);

            // 4. Lưu vào DB
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Admin đã bơm thêm {Quantity} sản phẩm cho FlashSaleItem {ItemId}. SaleStock mới: {NewStock}", 
                request.QuantityToAdd, request.FlashSaleItemId, item.SaleStock);

            return true;
        }
    }
}
