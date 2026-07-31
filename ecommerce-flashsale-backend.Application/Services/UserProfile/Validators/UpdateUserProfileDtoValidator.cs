using FluentValidation;
using FlashSale.Application.Services.UserProfile.DTOs;
using System.Text.RegularExpressions;

namespace FlashSale.Application.Services.UserProfile.Validators
{
    public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
    {
        public UpdateUserProfileDtoValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .Length(2, 100).WithMessage("Họ tên phải từ 2 đến 100 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .Must(BeAValidVietnamesePhoneNumber)
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Số điện thoại không hợp lệ (phải theo chuẩn Việt Nam, ví dụ: 0987654321).");
        }

        private bool BeAValidVietnamesePhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return true;

            // Regex cơ bản cho số điện thoại Việt Nam (10 số, bắt đầu bằng 03, 05, 07, 08, 09)
            var regex = new Regex(@"^(03|05|07|08|09)\d{8}$");
            return regex.IsMatch(phoneNumber);
        }
    }
}
