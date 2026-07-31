using System;
using System.Threading.Tasks;
using FlashSale.Application.Services.Auth.DTOs;

namespace FlashSale.Application.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResultDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResultDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto);
    }
}
