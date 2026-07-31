using System.Collections.Generic;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Common.Guards;

namespace FlashSale.Domain.Catalog.ValueObjects
{
    public class Money : ValueObject
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        private Money() { }

        public Money(decimal amount, string currency = "VND")
        {
            Guard.AgainstNegative(amount, nameof(amount));
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }
}
