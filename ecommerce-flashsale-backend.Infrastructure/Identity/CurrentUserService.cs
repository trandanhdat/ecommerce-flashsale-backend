using System;
using System.Security.Claims;
using FlashSale.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FlashSale.Infrastructure.Identity
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                // [CHEAT CODE] Dùng riêng cho K6 Load Testing để giả lập 500 User khác nhau
                if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Cheat-UserId", out var cheatId))
                {
                    if (Guid.TryParse(cheatId, out var parsedCheatId))
                        return parsedCheatId;
                }

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }

        public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
