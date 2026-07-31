using System;
using FluentValidation;
using FlashSale.Application.Services.Admin.DTOs;

namespace FlashSale.Application.Services.Admin.Validators
{
    public class UpdateBannerDtoValidator : AbstractValidator<UpdateBannerDto>
    {
        public UpdateBannerDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống.")
                .Length(2, 200).WithMessage("Tiêu đề phải từ 2 đến 200 ký tự.");

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Đường dẫn hình ảnh không được để trống.");

            // Kiểm tra ImageUrl phải là một URL hợp lệ nếu có nhập
            RuleFor(x => x.ImageUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrEmpty(x.ImageUrl))
                .WithMessage("Đường dẫn hình ảnh không hợp lệ.");

            // Nếu có nhập LinkUrl thì cũng phải là URL hợp lệ
            RuleFor(x => x.LinkUrl)
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrEmpty(x.LinkUrl))
                .WithMessage("Đường dẫn liên kết không hợp lệ.");

            // Kiểm tra ngày kết thúc phải sau ngày bắt đầu (nếu có cả 2)
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }
}
