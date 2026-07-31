using System;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Catalog.ValueObjects;
using FlashSale.Domain.FlashSales.Exceptions;
using FlashSale.Domain.FlashSales.Events;

namespace FlashSale.Domain.FlashSales
{
    public class FlashSaleItem : Entity
    {
        public Guid FlashSaleId { get; private set; }
        public Guid ProductId { get; private set; }
        public Money SalePrice { get; private set; }
        public int SaleStock { get; private set; }
        public int ReservedCount { get; private set; }
        public int SoldCount { get; private set; }
        public int MaxPerUser { get; private set; }
        public byte[] RowVersion { get; private set; }

        public FlashSale FlashSale { get; private set; }

        protected FlashSaleItem() { }

        public FlashSaleItem(Guid flashSaleId, Guid productId, Money salePrice, int saleStock, int maxPerUser)
        {
            Id = Guid.NewGuid();
            FlashSaleId = flashSaleId;
            ProductId = productId;
            SalePrice = salePrice;
            SaleStock = saleStock;
            MaxPerUser = maxPerUser;
        }

        public void ReserveStock(int quantity)
        {
            if (ReservedCount + SoldCount + quantity > SaleStock)
            {
                throw new FlashSaleStockExceededException(Id);
            }

            ReservedCount += quantity;
            AddDomainEvent(new FlashSaleItemStockDecrementedEvent(Id, quantity));

            if (ReservedCount + SoldCount == SaleStock)
            {
                AddDomainEvent(new FlashSaleItemSoldOutEvent(Id));
            }
        }

        public void UpdateSoldCount(int newSoldCount)
        {
            if (newSoldCount < 0 || newSoldCount > SaleStock)
                throw new DomainException("Invalid sold count for sync.");
            
            SoldCount = newSoldCount;
        }

        public void AddStock(int quantityToAdd)
        {
            if (quantityToAdd <= 0)
                throw new DomainException("Quantity to add must be greater than zero.");
            
            SaleStock += quantityToAdd;
        }
    }
}
