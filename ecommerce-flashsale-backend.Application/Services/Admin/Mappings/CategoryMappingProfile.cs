using AutoMapper;
using FlashSale.Domain.Catalog;

namespace FlashSale.Application.Services.Admin.Mappings
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<Category, DTOs.CategoryDto>();
        }
    }
}
