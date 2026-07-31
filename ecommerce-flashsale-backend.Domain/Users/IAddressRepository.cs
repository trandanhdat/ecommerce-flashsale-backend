using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlashSale.Domain.Users
{
    // Interface Domain thuần.
    // Lưu ý: Không kế thừa IRepository<Address> vì IRepository yêu cầu T : IAggregateRoot,
    // trong khi Address chỉ là Entity con của User.
    public interface IAddressRepository
    {
        Task<Address?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Address>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task<Address?> GetDefaultByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task AddAsync(Address address, CancellationToken ct = default);
        Task UpdateAsync(Address address, CancellationToken ct = default);
        Task DeleteAsync(Address address, CancellationToken ct = default);
    }
}
