using FluentValidation;

namespace FlashSale.Application.Services.Admin.Validators
{
    public class CreateCategoryDtoValidator : AbstractValidator<DTOs.CreateCategoryDto>
    {
        public CreateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống.")
                .Length(2, 100).WithMessage("Tên danh mục phải từ 2 đến 100 ký tự.");
        }
    }
}
