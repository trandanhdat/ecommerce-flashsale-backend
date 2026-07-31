using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlashSale.Application.Common.Interfaces;
using FlashSale.Application.Services.Cart.DTOs;
using FlashSale.Domain.Catalog;
using FlashSale.Domain.Orders;
using FlashSale.Domain.Users;
using FlashSale.Domain.SeedWork;
using Microsoft.Extensions.Logging;

namespace FlashSale.Application.Services.Cart
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ICartRepository cartRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ILogger<CartService> logger)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<CartDto> GetMyCartAsync(CancellationToken ct = default)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            var cartItems = await _cartRepository.GetByUserIdAsync(userId.Value, ct);
            if (!cartItems.Any()) return new CartDto();

            // Fetch products to get Name, ImageUrl, and Current Price
            var productIds = cartItems.Select(c => c.ProductId).Distinct().ToList();
            var products = await _productRepository.GetAsync(p => productIds.Contains(p.Id));
            var productDict = products.ToDictionary(p => p.Id);

            var dto = new CartDto();
            foreach (var item in cartItems)
            {
                if (productDict.TryGetValue(item.ProductId, out var product))
                {
                    var unitPrice = product.DiscountPrice.Amount > 0 ? product.DiscountPrice.Amount : product.BasePrice.Amount;
                    dto.Items.Add(new CartItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        ProductName = product.Name,
                        ProductImageUrl = product.ImageUrl,
                        UnitPrice = unitPrice,
                        TotalPrice = unitPrice * item.Quantity
                    });
                }
            }

            dto.TotalAmount = dto.Items.Sum(i => i.TotalPrice);
            return dto;
        }

        public async Task<bool> AddItemAsync(AddCartItemDto dto, CancellationToken ct = default)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null || !product.IsActive)
                throw new Exception("Product not found or inactive.");

            var existingItem = await _cartRepository.GetByUserAndProductAsync(userId.Value, dto.ProductId, ct);
            
            // Check nếu cộng dồn thì có vượt tồn kho không
            var newQuantity = (existingItem?.Quantity ?? 0) + dto.Quantity;
            if (newQuantity > product.StockQuantity)
                throw new Exception("Not enough stock available.");

            if (existingItem != null)
            {
                // Sản phẩm đã có trong giỏ -> Cộng dồn Quantity
                existingItem.UpdateQuantity(newQuantity);
            }
            else
            {
                // Sản phẩm chưa có trong giỏ -> Tạo mới
                var newItem = CartItem.Create(userId.Value, dto.ProductId, dto.Quantity);
                _cartRepository.Add(newItem);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateQuantityAsync(Guid cartItemId, int quantity, CancellationToken ct = default)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            var cartItem = await _cartRepository.GetByIdAsync(cartItemId);
            if (cartItem == null || cartItem.UserId != userId.Value)
                throw new Exception("Cart item not found or you are not the owner.");

            if (quantity <= 0)
            {
                await _cartRepository.DeleteAsync(cartItem);
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                if (quantity > product.StockQuantity)
                    throw new Exception("Not enough stock available.");

                cartItem.UpdateQuantity(quantity);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RemoveItemAsync(Guid cartItemId, CancellationToken ct = default)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            var cartItem = await _cartRepository.GetByIdAsync(cartItemId);
            if (cartItem == null || cartItem.UserId != userId.Value)
                throw new Exception("Cart item not found or you are not the owner.");

            await _cartRepository.DeleteAsync(cartItem);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ClearCartAsync(CancellationToken ct = default)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            var cartItems = await _cartRepository.GetByUserIdAsync(userId.Value, ct);
            _cartRepository.RemoveRange(cartItems);
            
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        public async Task<Guid> CheckoutAsync(CancellationToken ct = default)
        {
            var userId = _currentUserService.UserId;
            if (userId == null) throw new UnauthorizedAccessException();

            // a) Lấy CartItems hiện tại
            var cartItems = await _cartRepository.GetByUserIdAsync(userId.Value, ct);
            if (!cartItems.Any())
                throw new Exception("Cart is empty.");

            // b) Lấy danh sách Product mới nhất từ DB để validate lại giá và tồn kho
            var productIds = cartItems.Select(c => c.ProductId).ToList();
            var products = await _productRepository.GetAsync(p => productIds.Contains(p.Id));
            var productDict = products.ToDictionary(p => p.Id);

            // c) Tạo Order.Create(...) dạng Normal (PaymentDeadline = 30 minutes, no ReservationId)
            var paymentDeadline = DateTime.UtcNow.AddMinutes(30);
            var order = new Order(userId.Value, OrderType.Normal, "Customer Name", "Customer Phone", "Customer Address", paymentDeadline, null);

            foreach (var item in cartItems)
            {
                if (!productDict.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                    throw new Exception($"Product {item.ProductId} is no longer available.");

                // Validate lại StockQuantity tại thời điểm Checkout
                if (product.StockQuantity < item.Quantity)
                    throw new Exception($"Product {product.Name} does not have enough stock.");

                // Tính giá từ Product hiện tại (không tin tưởng giá cũ)
                var currentPrice = product.DiscountPrice.Amount > 0 ? product.DiscountPrice.Amount : product.BasePrice.Amount;
                order.AddOrderItem(product.Id, currentPrice, item.Quantity);

                // d) Trừ tồn kho thẳng vào DB (Sẽ được EF Core bảo vệ bằng Optimistic Concurrency qua thuộc tính RowVersion)
                product.AdjustStock(-item.Quantity);
            }

            // Lưu Order
            _orderRepository.Add(order);

            // e) Xoá giỏ hàng
            _cartRepository.RemoveRange(cartItems);

            // SaveChanges toàn bộ: Tạo Order + Cập nhật Product.StockQuantity + Xoá CartItems
            // Nếu có ai đó vừa mua mất hàng, Product.RowVersion thay đổi -> Quăng DbUpdateConcurrencyException
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                throw new Exception("Tồn kho sản phẩm đã bị thay đổi bởi người khác trong lúc bạn thanh toán. Vui lòng thử lại!");
            }

            // f) Trả về OrderId để FE chuyển sang /api/payments/initiate
            return order.Id;
        }
    }
}
