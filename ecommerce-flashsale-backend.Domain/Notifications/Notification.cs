using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Notifications
{
    public class Notification : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public string Title { get; private set; }
        public string Content { get; private set; }
        public NotificationType Type { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime CreatedAt { get; private set; }

        protected Notification() { }

        public Notification(Guid userId, string title, string content, NotificationType type)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Title = title;
            Content = content;
            Type = type;
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }
}
