using AutoMapper;
using FlashSale.Domain.Catalog;
using FlashSale.Application.Services.Admin.DTOs;

namespace FlashSale.Application.Services.Admin.Mappings
{
    public class BannerMappingProfile : Profile
    {
        public BannerMappingProfile()
        {
            CreateMap<Banner, BannerDto>();
        }
    }
}
