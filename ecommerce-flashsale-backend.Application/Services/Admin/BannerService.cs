using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.SeedWork;
using ecommerce_flashsale_backend.Application.Common.Dtos;
using FlashSale.Application.Services.Admin.DTOs;

namespace FlashSale.Application.Services.Admin
{
    public class BannerService : IBannerService
    {
        private readonly IBannerRepository _bannerRepository;
        private readonly IMapper _mapper;

        public BannerService(IBannerRepository bannerRepository, IMapper mapper)
        {
            _bannerRepository = bannerRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<BannerDto>> GetAllAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default)
        {
            var (items, totalCount) = await _bannerRepository.GetPagedAsync(page, pageSize, isActive, ct);
            
            var dtos = _mapper.Map<IEnumerable<BannerDto>>(items);
            
            return new PagedResult<BannerDto>
            {
                Items = dtos.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BannerDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            if (banner == null)
            {
                throw new DomainException("Không tìm thấy banner.");
            }

            return _mapper.Map<BannerDto>(banner);
        }

        public async Task<BannerDto> CreateAsync(CreateBannerDto dto, CancellationToken ct = default)
        {
            var banner = Banner.Create(
                dto.Title, 
                dto.ImageUrl, 
                dto.LinkUrl, 
                dto.DisplayOrder, 
                dto.StartDate, 
                dto.EndDate
            );

            await _bannerRepository.AddAsync(banner);

            return _mapper.Map<BannerDto>(banner);
        }

        public async Task UpdateAsync(Guid id, UpdateBannerDto dto, CancellationToken ct = default)
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            if (banner == null)
            {
                throw new DomainException("Không tìm thấy banner.");
            }

            banner.Update(
                dto.Title, 
                dto.ImageUrl, 
                dto.LinkUrl, 
                dto.DisplayOrder, 
                dto.StartDate, 
                dto.EndDate
            );

            await _bannerRepository.UpdateAsync(banner);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            if (banner == null)
            {
                throw new DomainException("Không tìm thấy banner.");
            }

            await _bannerRepository.DeleteAsync(banner);
        }

        public async Task ToggleActiveAsync(Guid id, CancellationToken ct = default)
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            if (banner == null)
            {
                throw new DomainException("Không tìm thấy banner.");
            }

            // Gọi Activate() / Deactivate() trên entity thay vì set field trực tiếp
            if (banner.IsActive)
            {
                banner.Deactivate();
            }
            else
            {
                banner.Activate();
            }

            await _bannerRepository.UpdateAsync(banner);
        }
    }
}
