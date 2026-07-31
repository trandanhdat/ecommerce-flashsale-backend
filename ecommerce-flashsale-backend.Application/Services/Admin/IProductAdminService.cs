using System;
using System.Threading;
using System.Threading.Tasks;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Services.Admin.DTOs;

namespace FlashSale.Application.Services.Admin
{
    public interface IProductAdminService
    {
        Task<PagedResult<ProductAdminDto>> GetAllAsync(Guid? categoryId, string search, int page, int pageSize, CancellationToken ct = default);
        Task<ProductAdminDto> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ProductAdminDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
        Task UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
