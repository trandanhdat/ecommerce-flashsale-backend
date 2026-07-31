using FlashSale.Application.Common.Interfaces;
using FlashSale.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FlashSale.Infrastructure.Identity
{
    public class IdentityPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<User> _hasher;

        public IdentityPasswordHasher()
        {
            _hasher = new PasswordHasher<User>();
        }

        public string Hash(string password)
        {
            // The first parameter is the user object, but since we are just hashing,
            // we can pass null. ASP.NET Core Identity's PasswordHasher handles this gracefully.
            return _hasher.HashPassword(null!, password);
        }

        public bool Verify(string hashedPassword, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
