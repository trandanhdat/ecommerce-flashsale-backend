using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Users;
using FlashSale.Application.Services.UserProfile.DTOs;

namespace FlashSale.Application.Services.UserProfile
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public AddressService(IAddressRepository addressRepository, ICurrentUserService currentUserService, IMapper mapper)
        {
            _addressRepository = addressRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");
            return userId.Value;
        }

        public async Task<IEnumerable<AddressDto>> GetMyAddressesAsync(CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var addresses = await _addressRepository.GetAllByUserIdAsync(userId, ct);
            return _mapper.Map<IEnumerable<AddressDto>>(addresses);
        }

        public async Task<AddressDto> GetByIdAsync(Guid addressId, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var address = await _addressRepository.GetByIdAsync(addressId, ct);

            // Xác thực Ownership (Phòng chống IDOR)
            if (address == null || address.UserId != userId)
                throw new DomainException("Không tìm thấy địa chỉ.");

            return _mapper.Map<AddressDto>(address);
        }

        public async Task<AddressDto> CreateAsync(CreateAddressDto dto, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            
            var existingAddresses = await _addressRepository.GetAllByUserIdAsync(userId, ct);
            bool isFirstAddress = !existingAddresses.Any();

            // Nếu là địa chỉ đầu tiên, bắt buộc phải là default. Nếu dto yêu cầu default, thì cũng là default.
            bool isDefault = isFirstAddress || dto.IsDefault;

            // Logic Unmark Default: Nếu địa chỉ mới này là default, các địa chỉ cũ phải bị bỏ default
            if (isDefault && existingAddresses.Any())
            {
                foreach (var addr in existingAddresses.Where(a => a.IsDefault))
                {
                    addr.UnmarkAsDefault();
                    await _addressRepository.UpdateAsync(addr, ct);
                }
            }

            var newAddress = Address.Create(
                userId, 
                dto.RecipientName, 
                dto.PhoneNumber, 
                dto.Province, 
                dto.District, 
                dto.Ward, 
                dto.DetailAddress, 
                isDefault);

            await _addressRepository.AddAsync(newAddress, ct);
            return _mapper.Map<AddressDto>(newAddress);
        }

        public async Task<AddressDto> UpdateAsync(Guid addressId, UpdateAddressDto dto, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var address = await _addressRepository.GetByIdAsync(addressId, ct);

            // Xác thực Ownership
            if (address == null || address.UserId != userId)
                throw new DomainException("Không tìm thấy địa chỉ.");

            address.Update(dto.RecipientName, dto.PhoneNumber, dto.Province, dto.District, dto.Ward, dto.DetailAddress);

            // Logic xử lý IsDefault
            if (dto.IsDefault && !address.IsDefault)
            {
                // Unmark các address khác
                var allAddresses = await _addressRepository.GetAllByUserIdAsync(userId, ct);
                foreach (var otherAddr in allAddresses.Where(a => a.Id != addressId && a.IsDefault))
                {
                    otherAddr.UnmarkAsDefault();
                    await _addressRepository.UpdateAsync(otherAddr, ct);
                }
                address.MarkAsDefault();
            }
            // Không cho phép tự ý tắt IsDefault nếu đang là mặc định (phải qua SetDefault của address khác)
            // Nếu người dùng cố tình gửi dto.IsDefault = false khi nó đang là true, ta chỉ bỏ qua.

            await _addressRepository.UpdateAsync(address, ct);
            return _mapper.Map<AddressDto>(address);
        }

        public async Task DeleteAsync(Guid addressId, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var address = await _addressRepository.GetByIdAsync(addressId, ct);

            if (address == null || address.UserId != userId)
                throw new DomainException("Không tìm thấy địa chỉ.");

            bool wasDefault = address.IsDefault;

            await _addressRepository.DeleteAsync(address, ct);

            // Nếu xoá address đang là mặc định, tìm địa chỉ còn lại để set làm mặc định mới
            if (wasDefault)
            {
                var remainingAddresses = await _addressRepository.GetAllByUserIdAsync(userId, ct);
                var newDefault = remainingAddresses.FirstOrDefault(); // Sẽ lấy Id lớn nhất do logic ThenByDescending(Id) ở Repo
                if (newDefault != null)
                {
                    newDefault.MarkAsDefault();
                    await _addressRepository.UpdateAsync(newDefault, ct);
                }
            }
        }

        public async Task SetDefaultAsync(Guid addressId, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var allAddresses = (await _addressRepository.GetAllByUserIdAsync(userId, ct)).ToList();

            var addressToMark = allAddresses.FirstOrDefault(a => a.Id == addressId);
            if (addressToMark == null)
                throw new DomainException("Không tìm thấy địa chỉ.");

            // Logic unmark và mark (có thể được EF Core xử lý tự động trong SaveChangesAsync gọi bên trong UpdateAsync)
            // Lặp qua để unmark những thằng đang là default
            foreach (var addr in allAddresses.Where(a => a.Id != addressId && a.IsDefault))
            {
                addr.UnmarkAsDefault();
                await _addressRepository.UpdateAsync(addr, ct);
            }

            addressToMark.MarkAsDefault();
            await _addressRepository.UpdateAsync(addressToMark, ct);
        }
    }
}
