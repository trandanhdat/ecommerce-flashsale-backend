using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.FlashSales.Events
{
    public class FlashSaleActivatedEvent : IDomainEvent
    {
        public Guid FlashSaleId { get; }
        public FlashSaleActivatedEvent(Guid flashSaleId) => FlashSaleId = flashSaleId;
    }

    public class FlashSaleEndedEvent : IDomainEvent
    {
        public Guid FlashSaleId { get; }
        public FlashSaleEndedEvent(Guid flashSaleId) => FlashSaleId = flashSaleId;
    }

    public class FlashSaleItemStockDecrementedEvent : IDomainEvent
    {
        public Guid FlashSaleItemId { get; }
        public int Quantity { get; }
        public FlashSaleItemStockDecrementedEvent(Guid flashSaleItemId, int quantity)
        {
            FlashSaleItemId = flashSaleItemId;
            Quantity = quantity;
        }
    }

    public class FlashSaleItemSoldOutEvent : IDomainEvent
    {
        public Guid FlashSaleItemId { get; }
        public FlashSaleItemSoldOutEvent(Guid flashSaleItemId) => FlashSaleItemId = flashSaleItemId;
    }
}
