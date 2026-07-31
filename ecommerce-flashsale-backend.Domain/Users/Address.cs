using System;
using System.Text.RegularExpressions;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users
{
    public class Address : Entity
    {
        public Guid UserId { get; private set; }
        public string RecipientName { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Province { get; private set; }
        public string District { get; private set; }
        public string Ward { get; private set; }
        public string DetailAddress { get; private set; }
        public bool IsDefault { get; private set; }

        public User User { get; private set; }

        // Constructor private cho EF Core
        protected Address() { }

        private Address(Guid userId, string recipientName, string phoneNumber, string province, string district, string ward, string detailAddress, bool isDefault)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            RecipientName = recipientName;
            PhoneNumber = phoneNumber;
            Province = province;
            District = district;
            Ward = ward;
            DetailAddress = detailAddress;
            IsDefault = isDefault;
        }

        /// <summary>
        /// Factory method để tạo Address với các bước validate cơ bản
        /// </summary>
        public static Address Create(Guid userId, string recipientName, string phoneNumber, string province, string district, string ward, string detailAddress, bool isDefault = false)
        {
            if (userId == Guid.Empty)
                throw new DomainException("UserId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(recipientName))
                throw new DomainException("Tên người nhận (RecipientName) không được để trống.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new DomainException("Số điện thoại (PhoneNumber) không được để trống.");

            // Validate số điện thoại VN (ví dụ: bắt đầu bằng số 0, có 10 chữ số)
            if (!Regex.IsMatch(phoneNumber, @"^(0[3|5|7|8|9])+([0-9]{8})$"))
                throw new DomainException("Số điện thoại không đúng định dạng Việt Nam.");

            if (string.IsNullOrWhiteSpace(province))
                throw new DomainException("Tỉnh/Thành phố (Province) không được để trống.");

            if (string.IsNullOrWhiteSpace(district))
                throw new DomainException("Quận/Huyện (District) không được để trống.");

            if (string.IsNullOrWhiteSpace(ward))
                throw new DomainException("Phường/Xã (Ward) không được để trống.");

            if (string.IsNullOrWhiteSpace(detailAddress))
                throw new DomainException("Địa chỉ chi tiết (DetailAddress) không được để trống.");

            return new Address(userId, recipientName, phoneNumber, province, district, ward, detailAddress, isDefault);
        }

        /// <summary>
        /// Đánh dấu địa chỉ này là địa chỉ mặc định
        /// </summary>
        public void MarkAsDefault()
        {
            IsDefault = true;
        }

        /// <summary>
        /// Bỏ đánh dấu địa chỉ mặc định
        /// </summary>
        public void UnmarkAsDefault()
        {
            IsDefault = false;
        }

        /// <summary>
        /// Cập nhật thông tin địa chỉ
        /// </summary>
        public void Update(string recipientName, string phoneNumber, string province, string district, string ward, string detailAddress)
        {
            if (string.IsNullOrWhiteSpace(recipientName))
                throw new DomainException("Tên người nhận (RecipientName) không được để trống.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new DomainException("Số điện thoại (PhoneNumber) không được để trống.");

            if (!Regex.IsMatch(phoneNumber, @"^(0[3|5|7|8|9])+([0-9]{8})$"))
                throw new DomainException("Số điện thoại không đúng định dạng Việt Nam.");

            if (string.IsNullOrWhiteSpace(province))
                throw new DomainException("Tỉnh/Thành phố (Province) không được để trống.");

            if (string.IsNullOrWhiteSpace(district))
                throw new DomainException("Quận/Huyện (District) không được để trống.");

            if (string.IsNullOrWhiteSpace(ward))
                throw new DomainException("Phường/Xã (Ward) không được để trống.");

            if (string.IsNullOrWhiteSpace(detailAddress))
                throw new DomainException("Địa chỉ chi tiết (DetailAddress) không được để trống.");

            RecipientName = recipientName;
            PhoneNumber = phoneNumber;
            Province = province;
            District = district;
            Ward = ward;
            DetailAddress = detailAddress;
        }
    }
}
