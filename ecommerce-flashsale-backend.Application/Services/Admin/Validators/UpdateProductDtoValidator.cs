using System;
using FlashSale.Application.Services.Admin.DTOs;
using FluentValidation;

namespace FlashSale.Application.Services.Admin.Validators
{
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(2, 200).WithMessage("Name must be between 2 and 200 characters.");

            RuleFor(x => x.BasePrice)
                .GreaterThan(0).WithMessage("BasePrice must be greater than 0.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("StockQuantity cannot be negative.");

            RuleFor(x => x.CategoryId)
                .NotEqual(Guid.Empty).WithMessage("CategoryId is required.");

            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU is required.");
        }
    }
}
