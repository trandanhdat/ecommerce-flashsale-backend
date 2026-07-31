using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users.Events
{
    public class UserRegisteredEvent : IDomainEvent
    {
        public Guid UserId { get; }

        public UserRegisteredEvent(Guid userId)
        {
            UserId = userId;
        }
    }
}
