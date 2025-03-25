using System;
using System.Collections.Generic;

namespace MaintainEase.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents a physical address as a value object
    /// </summary>
    public class Address : ValueObject
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string State { get; private set; }
        public string PostalCode { get; private set; }
        public string Country { get; private set; }
        public string BuildingNumber { get; private set; }
        public string ApartmentNumber { get; private set; }

        // For EF Core
        private Address() { }

        public Address(
            string street,
            string city,
            string state,
            string postalCode,
            string country,
            string buildingNumber = null,
            string apartmentNumber = null)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be empty", nameof(street));

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be empty", nameof(city));

            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("State cannot be empty", nameof(state));

            if (string.IsNullOrWhiteSpace(postalCode))
                throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));

            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country cannot be empty", nameof(country));

            Street = street;
            City = city;
            State = state;
            PostalCode = postalCode;
            Country = country;
            BuildingNumber = buildingNumber;
            ApartmentNumber = apartmentNumber;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return State;
            yield return PostalCode;
            yield return Country;
            yield return BuildingNumber;
            yield return ApartmentNumber;
        }

        public override string ToString()
        {
            var addressParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(BuildingNumber))
                addressParts.Add(BuildingNumber);

            addressParts.Add(Street);

            if (!string.IsNullOrWhiteSpace(ApartmentNumber))
                addressParts.Add($"Apt {ApartmentNumber}");

            addressParts.Add(City);
            addressParts.Add($"{State} {PostalCode}");
            addressParts.Add(Country);

            return string.Join(", ", addressParts);
        }
    }
}
