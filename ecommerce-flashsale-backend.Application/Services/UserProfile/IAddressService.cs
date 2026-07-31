using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Services.UserProfile.DTOs;

namespace FlashSale.Application.Services.UserProfile
{
    public interface IAddressService
    {
        Task<IEnumerable<AddressDto>> GetMyAddressesAsync(CancellationToken ct = default);
        Task<AddressDto> GetByIdAsync(Guid addressId, CancellationToken ct = default);
        Task<AddressDto> CreateAsync(CreateAddressDto dto, CancellationToken ct = default);
        Task<AddressDto> UpdateAsync(Guid addressId, UpdateAddressDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid addressId, CancellationToken ct = default);
        Task SetDefaultAsync(Guid addressId, CancellationToken ct = default);
    }
}
