using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class CartRepository : RepositoryBase<CartItem>, ICartRepository
    {
        public CartRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(x => x.UserId == userId)
                .ToListAsync(ct);
        }

        public async Task<CartItem?> GetByUserAndProductAsync(Guid userId, Guid productId, CancellationToken ct = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId, ct);
        }

        public void RemoveRange(IEnumerable<CartItem> cartItems)
        {
            _dbSet.RemoveRange(cartItems);
        }
    }
}
