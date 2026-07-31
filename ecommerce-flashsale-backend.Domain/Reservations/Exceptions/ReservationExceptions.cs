using System;
using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Reservations.Exceptions
{
    public class ReservationAlreadyExpiredException : DomainException
    {
        public ReservationAlreadyExpiredException(Guid reservationId)
            : base($"Reservation {reservationId} has already expired.")
        {
        }
    }
}
