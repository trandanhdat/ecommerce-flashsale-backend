using FlashSale.Domain.Notifications;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext dbContext) : base(dbContext) { }
    }
}
