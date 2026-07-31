using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace FlashSale.Application.CQRS.FlashSaleOrders.Commands.PlaceFlashSaleOrder
{
    public class PlaceFlashSaleOrderCommandValidator : AbstractValidator<PlaceFlashSaleOrderCommand>
    {
        public PlaceFlashSaleOrderCommandValidator(IConfiguration configuration)
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");

            int maxQuantity = int.Parse(configuration["FlashSale:MaxQuantityPerOrder"] ?? "5");

            RuleFor(x => x.Quantity)
                .LessThanOrEqualTo(maxQuantity)
                .WithMessage($"Số lượng tối đa mỗi lần mua là {maxQuantity}.");
        }
    }
}
