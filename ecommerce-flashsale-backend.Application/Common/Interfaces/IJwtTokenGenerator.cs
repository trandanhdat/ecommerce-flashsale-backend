using System;
using FlashSale.Domain.Users;

namespace FlashSale.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        (string accessToken, DateTime expiresAt) GenerateAccessToken(User user, string role);
        string GenerateRefreshToken();
    }
}
