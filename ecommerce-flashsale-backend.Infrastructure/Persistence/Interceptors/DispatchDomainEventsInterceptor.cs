using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Domain.SeedWork;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FlashSale.Infrastructure.Persistence.Interceptors
{
    public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
    {
        private readonly IMediator _mediator;

        public DispatchDomainEventsInterceptor(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private async Task DispatchDomainEventsAsync(Microsoft.EntityFrameworkCore.DbContext? context, CancellationToken cancellationToken)
        {
            if (context == null) return;

            var entitiesWithEvents = context.ChangeTracker
                .Entries<Entity>()
                .Where(e => e.Entity.DomainEvents != null && e.Entity.DomainEvents.Any())
                .ToList();

            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.Entity.DomainEvents!)
                .ToList();

            entitiesWithEvents.ForEach(e => e.Entity.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
