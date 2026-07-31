using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Services.UserProfile.DTOs;

namespace FlashSale.Application.Services.UserProfile
{
    public interface IUserProfileService
    {
        Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default);
        Task<UserProfileDto> UpdateMyProfileAsync(UpdateUserProfileDto dto, CancellationToken ct = default);
    }
}
