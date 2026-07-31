using AutoMapper;
using FlashSale.Application.Services.Admin.DTOs;
using FlashSale.Domain.Catalog;

namespace FlashSale.Application.Services.Admin.Mappings
{
    public class ProductMappingProfile : Profile
    {
        public ProductMappingProfile()
        {
            CreateMap<Product, ProductAdminDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.BasePrice != null ? src.BasePrice.Amount : 0))
                .ForMember(dest => dest.DiscountPrice, opt => opt.MapFrom(src => src.DiscountPrice != null ? src.DiscountPrice.Amount : 0));
        }
    }
}
