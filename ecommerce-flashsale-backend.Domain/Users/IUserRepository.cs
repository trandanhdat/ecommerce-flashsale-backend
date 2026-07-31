using FlashSale.Domain.SeedWork;
using System.Threading.Tasks;
namespace FlashSale.Domain.Users
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
