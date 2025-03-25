using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.ValueObjects
{
    /// <summary>
    /// Represents an Israeli ID (Teudat Zehut) as a value object
    /// </summary>
    public class TeudatZehut : ValueObject
    {
        public string IdNumber { get; private set; }

        // For EF Core
        private TeudatZehut() { }

        public TeudatZehut(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                throw new ArgumentException("ID number cannot be empty", nameof(idNumber));

            // Remove any non-numeric characters
            idNumber = Regex.Replace(idNumber, "[^0-9]", "");

            if (idNumber.Length != 9)
                throw new ArgumentException("Israeli ID number must be 9 digits", nameof(idNumber));

            if (!IsValidIsraeliId(idNumber))
                throw new ArgumentException("Invalid Israeli ID number", nameof(idNumber));

            IdNumber = idNumber;
        }

        // Implement the Israeli ID validation algorithm
        private bool IsValidIsraeliId(string id)
        {
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                int digit = id[i] - '0';
                if (i % 2 == 0)
                {
                    sum += digit;
                }
                else
                {
                    digit *= 2;
                    sum += digit / 10 + digit % 10;
                }
            }
            return sum % 10 == 0;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return IdNumber;
        }

        public override string ToString()
        {
            return IdNumber;
        }
    }
}
