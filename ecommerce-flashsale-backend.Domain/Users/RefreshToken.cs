using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users
{
    public class RefreshToken : Entity
    {
        public Guid UserId { get; private set; }
        public string Token { get; private set; }
        public DateTime Expires { get; private set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; private set; }
        public DateTime? Revoked { get; private set; }
        public bool IsActive => Revoked == null && !IsExpired;

        public User User { get; private set; }

        protected RefreshToken() { } // EF Core

        public RefreshToken(Guid userId, string token, DateTime expires)
        {
            UserId = userId;
            Token = token;
            Expires = expires;
            Created = DateTime.UtcNow;
        }

        public void Revoke()
        {
            Revoked = DateTime.UtcNow;
        }
    }
}
