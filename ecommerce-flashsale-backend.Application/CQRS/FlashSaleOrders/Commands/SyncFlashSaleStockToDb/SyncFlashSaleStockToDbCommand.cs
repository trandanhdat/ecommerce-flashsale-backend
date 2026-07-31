using MediatR;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.SyncFlashSaleStockToDb
{
    // Sync tất cả FlashSale đang Active
    public record SyncFlashSaleStockToDbCommand() : IRequest;
}
