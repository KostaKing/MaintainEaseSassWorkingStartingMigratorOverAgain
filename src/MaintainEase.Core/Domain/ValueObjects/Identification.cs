using System;
using System.Collections.Generic;

namespace MaintainEase.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents an identification document as a value object
    /// </summary>
    public class Identification : ValueObject
    {
        public string IdNumber { get; private set; }
        public string IdType { get; private set; }
        public string IssuingCountry { get; private set; }
        public DateTimeOffset ExpirationDate { get; private set; }

        // For EF Core
        private Identification() { }

        public Identification(
            string idNumber,
            string idType,
            string issuingCountry,
            DateTimeOffset expirationDate)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                throw new ArgumentException("ID number cannot be empty", nameof(idNumber));

            if (string.IsNullOrWhiteSpace(idType))
                throw new ArgumentException("ID type cannot be empty", nameof(idType));

            if (string.IsNullOrWhiteSpace(issuingCountry))
                throw new ArgumentException("Issuing country cannot be empty", nameof(issuingCountry));

            IdNumber = idNumber;
            IdType = idType;
            IssuingCountry = issuingCountry;
            ExpirationDate = expirationDate;
        }

        public bool IsExpired()
        {
            return DateTimeOffset.UtcNow > ExpirationDate;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return IdNumber;
            yield return IdType;
            yield return IssuingCountry;
            yield return ExpirationDate;
        }

        public override string ToString()
        {
            return $"{IdType}: {IdNumber} ({IssuingCountry})";
        }
    }
}
