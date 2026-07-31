using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlashSale.Infrastructure.Messaging
{
    public class NoOpEventPublisher : IEventPublisher
    {
        private readonly ILogger<NoOpEventPublisher> _logger;

        public NoOpEventPublisher(ILogger<NoOpEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
        {
            _logger.LogInformation("NoOpEventPublisher: Giả lập publish sự kiện {EventName}.", typeof(T).Name);
            return Task.CompletedTask;
        }
    }
}
