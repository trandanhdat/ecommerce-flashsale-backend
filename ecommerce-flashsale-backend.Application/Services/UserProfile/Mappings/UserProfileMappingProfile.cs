using AutoMapper;
using FlashSale.Domain.Users;
using FlashSale.Application.Services.UserProfile.DTOs;

namespace FlashSale.Application.Services.UserProfile.Mappings
{
    public class UserProfileMappingProfile : Profile
    {
        public UserProfileMappingProfile()
        {
            // Bỏ qua map Role vì Role có thể lấy từ Claims hoặc xử lý riêng bên ngoài (IUserRepository không trả về Role trực tiếp)
            CreateMap<User, UserProfileDto>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());
        }
    }
}
