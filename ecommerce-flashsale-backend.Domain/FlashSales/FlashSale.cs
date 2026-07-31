using System;
using System.Collections.Generic;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.FlashSales.Exceptions;
using FlashSale.Domain.FlashSales.Events;
using FlashSale.Domain.FlashSales.Specifications;

namespace FlashSale.Domain.FlashSales
{
    public class FlashSale : AggregateRoot
    {
        public string Title { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public FlashSaleStatus Status { get; private set; }

        public ICollection<FlashSaleItem> Items { get; private set; } = new List<FlashSaleItem>();

        protected FlashSale() { }

        public FlashSale(string title, DateTime startTime, DateTime endTime)
        {
            Id = Guid.NewGuid();
            Title = title;
            StartTime = startTime;
            EndTime = endTime;
            Status = FlashSaleStatus.Upcoming;
        }

        public void Activate()
        {
            var spec = new FlashSaleCanBeActivatedSpec();
            if (!spec.IsSatisfiedBy(this))
            {
                throw new DomainException("Flash sale cannot be activated at this time.");
            }

            Status = FlashSaleStatus.Active;
            AddDomainEvent(new FlashSaleActivatedEvent(Id));
        }

        public void End()
        {
            Status = FlashSaleStatus.Ended;
            AddDomainEvent(new FlashSaleEndedEvent(Id));
        }
    }
}
