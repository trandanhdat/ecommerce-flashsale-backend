using MediatR;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.ExpireReservations
{
    // Chạy định kỳ, quét toàn bộ Reservation hết hạn
    public record ExpireReservationsCommand() : IRequest;
}
