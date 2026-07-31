using FluentValidation;

namespace FlashSale.Application.Services.Admin.Validators
{
    public class UpdateCategoryDtoValidator : AbstractValidator<DTOs.UpdateCategoryDto>
    {
        public UpdateCategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên danh mục không được để trống.")
                .Length(2, 100).WithMessage("Tên danh mục phải từ 2 đến 100 ký tự.");
        }
    }
}
