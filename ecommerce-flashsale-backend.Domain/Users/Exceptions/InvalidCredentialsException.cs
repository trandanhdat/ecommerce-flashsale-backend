using FlashSale.Domain.SeedWork;

namespace FlashSale.Domain.Users.Exceptions
{
    public class InvalidCredentialsException : DomainException
    {
        public InvalidCredentialsException(string email)
            : base($"Invalid credentials for user {email}.")
        {
        }
    }
}
