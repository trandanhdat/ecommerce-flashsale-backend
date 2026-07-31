using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Users;
using FlashSale.Application.Services.UserProfile.DTOs;

namespace FlashSale.Application.Services.UserProfile
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public UserProfileService(IUserRepository userRepository, ICurrentUserService currentUserService, IMapper mapper)
        {
            _userRepository = userRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
        {
            // Lấy ID người dùng từ Token thông qua ICurrentUserService
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập hoặc token không hợp lệ.");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                throw new DomainException("Không tìm thấy thông tin người dùng.");
            }

            var dto = _mapper.Map<UserProfileDto>(user);
            
            // Gán thêm Role từ Claims (do Entity User không chứa sẵn property Role trong IUserRepository)
            dto.Role = _currentUserService.Role;
            
            return dto;
        }

        public async Task<UserProfileDto> UpdateMyProfileAsync(UpdateUserProfileDto dto, CancellationToken ct = default)
        {
            // Lấy ID người dùng từ Token thông qua ICurrentUserService
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập hoặc token không hợp lệ.");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                throw new DomainException("Không tìm thấy thông tin người dùng.");
            }

            // Gọi phương thức thay đổi profile bên trong Domain Entity (Đúng nguyên tắc DDD)
            user.UpdateProfile(dto.FullName, dto.PhoneNumber);

            await _userRepository.UpdateAsync(user);

            var resultDto = _mapper.Map<UserProfileDto>(user);
            resultDto.Role = _currentUserService.Role;

            return resultDto;
        }
    }
}
