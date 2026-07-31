using System;
using System.Threading;
using System.Threading.Tasks;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Services.Admin.DTOs;

namespace FlashSale.Application.Services.Admin
{
    public interface ICategoryService
    {
        Task<PagedResult<CategoryDto>> GetAllAsync(int page, int pageSize, string search, CancellationToken ct = default);
        Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
        Task UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
