using Microsoft.AspNetCore.Identity;
using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users
{
    // User is an AggregateRoot, but since we are using ASP.NET Core Identity,
    // we inherit from IdentityUser<Guid> and implement IAggregateRoot if needed.
    // In this DDD model, we don't strictly inherit from AggregateRoot base class 
    // to avoid multiple inheritance issues, but we treat it as an Aggregate Root.
    public class User : IdentityUser<Guid>, IAggregateRoot
    {
        public string FullName { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private List<IDomainEvent> _domainEvents;
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly();

        public void AddDomainEvent(IDomainEvent eventItem)
        {
            _domainEvents = _domainEvents ?? new List<IDomainEvent>();
            _domainEvents.Add(eventItem);
        }

        public void RemoveDomainEvent(IDomainEvent eventItem)
        {
            _domainEvents?.Remove(eventItem);
        }

        public void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }

        protected User() { } // EF Core

        public User(string userName, string email, string fullName)
        {
            Id = Guid.NewGuid();
            UserName = userName;
            Email = email;
            FullName = fullName;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void UpdateProfile(string fullName, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Họ tên không được để trống.", nameof(fullName));

            FullName = fullName;
            PhoneNumber = phoneNumber; // Số điện thoại có thể null hoặc rỗng
        }
    }
}
