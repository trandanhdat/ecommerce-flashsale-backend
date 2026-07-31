using System;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Services.Cart.DTOs;

namespace FlashSale.Application.Services.Cart
{
    public interface ICartService
    {
        Task<CartDto> GetMyCartAsync(CancellationToken ct = default);
        Task<bool> AddItemAsync(AddCartItemDto dto, CancellationToken ct = default);
        Task<bool> UpdateQuantityAsync(Guid cartItemId, int quantity, CancellationToken ct = default);
        Task<bool> RemoveItemAsync(Guid cartItemId, CancellationToken ct = default);
        Task<bool> ClearCartAsync(CancellationToken ct = default);
        Task<Guid> CheckoutAsync(CancellationToken ct = default);
    }
}
