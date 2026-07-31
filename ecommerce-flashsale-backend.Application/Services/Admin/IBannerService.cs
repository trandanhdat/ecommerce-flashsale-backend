using System;
using System.Threading;
using System.Threading.Tasks;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Services.Admin.DTOs;

namespace FlashSale.Application.Services.Admin
{
    public interface IBannerService
    {
        Task<PagedResult<BannerDto>> GetAllAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default);
        Task<BannerDto> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<BannerDto> CreateAsync(CreateBannerDto dto, CancellationToken ct = default);
        Task UpdateAsync(Guid id, UpdateBannerDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task ToggleActiveAsync(Guid id, CancellationToken ct = default);
    }
}
