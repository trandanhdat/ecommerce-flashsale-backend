using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AddressRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Address?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Addresses.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<IEnumerable<Address>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _dbContext.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault) // Default luôn nằm đầu
                .ThenByDescending(a => a.Id) // Id (thay thế cho CreatedAt vì không có trường đó)
                .ToListAsync(ct);
        }

        public async Task<Address?> GetDefaultByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _dbContext.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .FirstOrDefaultAsync(ct);
        }

        public async Task AddAsync(Address address, CancellationToken ct = default)
        {
            await _dbContext.Addresses.AddAsync(address, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Address address, CancellationToken ct = default)
        {
            _dbContext.Addresses.Update(address);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Address address, CancellationToken ct = default)
        {
            _dbContext.Addresses.Remove(address);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
