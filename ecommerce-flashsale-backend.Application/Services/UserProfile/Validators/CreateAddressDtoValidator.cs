using FluentValidation;
using FlashSale.Application.Services.UserProfile.DTOs;
using System.Text.RegularExpressions;

namespace FlashSale.Application.Services.UserProfile.Validators
{
    public class CreateAddressDtoValidator : AbstractValidator<CreateAddressDto>
    {
        public CreateAddressDtoValidator()
        {
            RuleFor(x => x.RecipientName).NotEmpty().WithMessage("Tên người nhận không được để trống.");
            RuleFor(x => x.Province).NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.");
            RuleFor(x => x.District).NotEmpty().WithMessage("Quận/Huyện không được để trống.");
            RuleFor(x => x.Ward).NotEmpty().WithMessage("Phường/Xã không được để trống.");
            RuleFor(x => x.DetailAddress).NotEmpty().WithMessage("Địa chỉ chi tiết không được để trống.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Must(BeAValidVietnamesePhoneNumber).WithMessage("Số điện thoại không hợp lệ (phải theo chuẩn Việt Nam).");
        }

        private bool BeAValidVietnamesePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            var regex = new Regex(@"^(0[3|5|7|8|9])+([0-9]{8})$");
            return regex.IsMatch(phoneNumber);
        }
    }
}
