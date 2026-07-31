using System;
using FlashSale.Domain.SeedWork;
using FlashSale.Domain.Payments.Events;

namespace FlashSale.Domain.Payments
{
    public class Payment : AggregateRoot
    {
        public Guid OrderId { get; private set; }
        public PaymentProvider Provider { get; private set; }
        public string TransactionNo { get; private set; }
        public decimal Amount { get; private set; }
        public PaymentStatus Status { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public byte[] RowVersion { get; private set; }

        protected Payment() { }

        public Payment(Guid orderId, PaymentProvider provider, decimal amount)
        {
            Id = Guid.NewGuid();
            OrderId = orderId;
            Provider = provider;
            Amount = amount;
            Status = PaymentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            TransactionNo = Id.ToString("N"); // Khởi tạo giá trị mặc định để tránh lỗi NOT NULL trong DB
        }

        public void MarkAsSuccess(string transactionNo)
        {
            if (Status == PaymentStatus.Success) return;

            TransactionNo = transactionNo;
            Status = PaymentStatus.Success;
            PaidAt = DateTime.UtcNow;

            AddDomainEvent(new PaymentSucceededEvent(Id, OrderId));
        }

        public void MarkAsFailed(string transactionNo)
        {
            if (Status == PaymentStatus.Failed) return;

            TransactionNo = transactionNo;
            Status = PaymentStatus.Failed;

            AddDomainEvent(new PaymentFailedEvent(Id, OrderId));
        }
    }
}
