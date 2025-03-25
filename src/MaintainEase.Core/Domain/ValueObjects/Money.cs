using System;
using System.Collections.Generic;
using System.Globalization;

namespace MaintainEase.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents money as a value object with amount and currency
    /// </summary>
    public class Money : ValueObject
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        // For EF Core
        private Money() { }

        public Money(decimal amount, string currency = "USD")
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency code cannot be empty", nameof(currency));

            Amount = amount;
            Currency = currency;
        }

        public static Money operator +(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot add money with different currencies");

            return new Money(left.Amount + right.Amount, left.Currency);
        }

        public static Money operator -(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot subtract money with different currencies");

            return new Money(left.Amount - right.Amount, left.Currency);
        }

        public static Money operator *(Money left, decimal multiplier)
        {
            return new Money(left.Amount * multiplier, left.Currency);
        }

        public static Money operator /(Money left, decimal divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException();

            return new Money(left.Amount / divisor, left.Currency);
        }

        public static bool operator >(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot compare money with different currencies");

            return left.Amount > right.Amount;
        }

        public static bool operator <(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot compare money with different currencies");

            return left.Amount < right.Amount;
        }

        public static bool operator >=(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot compare money with different currencies");

            return left.Amount >= right.Amount;
        }

        public static bool operator <=(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Cannot compare money with different currencies");

            return left.Amount <= right.Amount;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString()
        {
            return $"{Amount.ToString("F2", CultureInfo.InvariantCulture)} {Currency}";
        }
    }
}
