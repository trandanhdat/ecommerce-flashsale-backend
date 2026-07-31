using System.Collections.Generic;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users.ValueObjects
{
    public class PhoneNumber : ValueObject
    {
        public string Value { get; private set; }

        private PhoneNumber() { }

        public PhoneNumber(string value)
        {
            // Simple validation, can be enhanced
            if (string.IsNullOrWhiteSpace(value) || value.Length < 9)
            {
                throw new DomainException("Invalid phone number.");
            }
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
