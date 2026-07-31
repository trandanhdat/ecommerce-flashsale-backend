using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users.Events
{
    public class PasswordChangedEvent : IDomainEvent
    {
        public Guid UserId { get; }

        public PasswordChangedEvent(Guid userId)
        {
            UserId = userId;
        }
    }
}
