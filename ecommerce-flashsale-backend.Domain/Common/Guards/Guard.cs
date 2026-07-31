using System;

namespace FlashSale.Domain.Common.Guards
{
    public static class Guard
    {
        public static void AgainstNull(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        public static void AgainstNullOrEmpty(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Parameter {parameterName} cannot be null or empty", parameterName);
            }
        }

        public static void AgainstNegative(decimal value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentException($"Parameter {parameterName} cannot be negative", parameterName);
            }
        }

        public static void AgainstNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentException($"Parameter {parameterName} cannot be negative", parameterName);
            }
        }
    }
}
