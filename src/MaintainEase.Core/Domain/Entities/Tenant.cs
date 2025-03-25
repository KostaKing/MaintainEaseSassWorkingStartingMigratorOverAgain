using System;
using MaintainEase.Core.Domain.Aggregates;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.Entities
{
    /// <summary>
    /// Represents a tenant who rents a property
    /// </summary>
    public class Tenant : AggregateRoot
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public Identification IdDocument { get; private set; }
        public DateTimeOffset RegistrationDate { get; private set; }
        public Address PermanentAddress { get; private set; }
        public bool IsActive { get; private set; }
        public int CreditScore { get; private set; }
        public string EmergencyContactName { get; private set; }
        public string EmergencyContactPhone { get; private set; }

        // For EF Core
        protected Tenant() { }

        public Tenant(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            Identification idDocument,
            Address permanentAddress,
            int creditScore = 0,
            string emergencyContactName = null,
            string emergencyContactPhone = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty", nameof(lastName));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            IdDocument = idDocument ?? throw new ArgumentNullException(nameof(idDocument));
            PermanentAddress = permanentAddress ?? throw new ArgumentNullException(nameof(permanentAddress));
            RegistrationDate = DateTimeOffset.UtcNow;
            IsActive = true;
            CreditScore = creditScore;
            EmergencyContactName = emergencyContactName;
            EmergencyContactPhone = emergencyContactPhone;

            AddDomainEvent(new TenantCreatedEvent(this));
        }

        public void UpdateContactInformation(
            string email,
            string phoneNumber,
            Address permanentAddress)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

            Email = email;
            PhoneNumber = phoneNumber;
            PermanentAddress = permanentAddress ?? throw new ArgumentNullException(nameof(permanentAddress));

            AddDomainEvent(new TenantContactInformationUpdatedEvent(this));
        }

        public void UpdateEmergencyContact(string name, string phone)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Emergency contact name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Emergency contact phone cannot be empty", nameof(phone));

            EmergencyContactName = name;
            EmergencyContactPhone = phone;
        }

        public void UpdateCreditScore(int creditScore)
        {
            if (creditScore < 0)
                throw new ArgumentException("Credit score cannot be negative", nameof(creditScore));

            CreditScore = creditScore;
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            AddDomainEvent(new TenantDeactivatedEvent(this));
        }

        public void Reactivate()
        {
            if (IsActive)
                return;

            IsActive = true;
            AddDomainEvent(new TenantReactivatedEvent(this));
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
