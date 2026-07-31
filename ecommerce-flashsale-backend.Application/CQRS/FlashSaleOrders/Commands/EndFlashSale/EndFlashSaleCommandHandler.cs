using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.FlashSales;
using FlashSale.Domain.SeedWork;
using FlashSale.Application.CQRS.FlashSaleOrders.Commands.SyncFlashSaleStockToDb;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.EndFlashSale
{
    public class EndFlashSaleCommandHandler : IRequestHandler<EndFlashSaleCommand>
    {
        private readonly IFlashSaleRepository _flashSaleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<EndFlashSaleCommandHandler> _logger;

        public EndFlashSaleCommandHandler(
            IFlashSaleRepository flashSaleRepository,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<EndFlashSaleCommandHandler> logger)
        {
            _flashSaleRepository = flashSaleRepository;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(EndFlashSaleCommand request, CancellationToken cancellationToken)
        {
            var flashSale = await _flashSaleRepository.GetByIdWithItemsAsync(request.FlashSaleId, cancellationToken);
            if (flashSale == null) return;

            // a) Kiểm tra Status = Active và EndTime <= UtcNow (idempotent)
            if (flashSale.Status != FlashSaleStatus.Active || flashSale.EndTime > DateTime.UtcNow)
            {
                _logger.LogInformation("Job EndFlashSale bỏ qua FlashSale {FlashSaleId} (Status: {Status}, EndTime: {EndTime}) vì đã kết thúc hoặc chưa tới giờ.", flashSale.Id, flashSale.Status, flashSale.EndTime);
                return;
            }

            // b) Gọi SyncFlashSaleStockToDb TRƯỚC khi đóng sale (gọi chung tất cả Active Sales)
            await _mediator.Send(new SyncFlashSaleStockToDbCommand(), cancellationToken);

            // c) Cập nhật FlashSale.Status = Ended qua method domain End()
            flashSale.End();

            // d) SaveChanges
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã kết thúc thành công FlashSale {FlashSaleId} và đồng bộ tồn kho.", flashSale.Id);
        }
    }
}
