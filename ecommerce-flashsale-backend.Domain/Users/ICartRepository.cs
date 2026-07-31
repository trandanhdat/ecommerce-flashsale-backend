using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users
{
    public interface ICartRepository : IRepository<CartItem>
    {
        Task<List<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<CartItem?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken ct = default);
        void RemoveRange(IEnumerable<CartItem> cartItems);
    }
}
