using System;
using System.Collections.Generic;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.Entities
{
    /// <summary>
    /// Represents a house committee (Vaad Bayit) for a building
    /// </summary>
    public class VaadBayit : Entity
    {
        private readonly List<Guid> _committeeMembers = new();
        private readonly List<Guid> _buildingPropertyIds = new();

        public string BuildingName { get; private set; }
        public Address BuildingAddress { get; private set; }
        public Guid ChairpersonId { get; private set; }
        public Guid TreasurerId { get; private set; }
        public Money MonthlyFeePerUnit { get; private set; }
        public Money EmergencyFund { get; private set; }
        public DateTimeOffset EstablishmentDate { get; private set; }
        public IReadOnlyCollection<Guid> CommitteeMembers => _committeeMembers.AsReadOnly();
        public IReadOnlyCollection<Guid> BuildingPropertyIds => _buildingPropertyIds.AsReadOnly();

        // For EF Core
        protected VaadBayit() { }

        public VaadBayit(
            string buildingName,
            Address buildingAddress,
            Guid chairpersonId,
            Guid treasurerId,
            Money monthlyFeePerUnit,
            Money emergencyFund,
            IEnumerable<Guid> buildingPropertyIds = null)
        {
            if (string.IsNullOrWhiteSpace(buildingName))
                throw new ArgumentException("Building name cannot be empty", nameof(buildingName));

            BuildingName = buildingName;
            BuildingAddress = buildingAddress ?? throw new ArgumentNullException(nameof(buildingAddress));
            ChairpersonId = chairpersonId;
            TreasurerId = treasurerId;
            MonthlyFeePerUnit = monthlyFeePerUnit ?? throw new ArgumentNullException(nameof(monthlyFeePerUnit));
            EmergencyFund = emergencyFund ?? throw new ArgumentNullException(nameof(emergencyFund));
            EstablishmentDate = DateTimeOffset.UtcNow;

            _committeeMembers.Add(chairpersonId);
            _committeeMembers.Add(treasurerId);

            if (buildingPropertyIds != null)
                _buildingPropertyIds.AddRange(buildingPropertyIds);
        }

        public void UpdateMonthlyFee(Money newFee)
        {
            if (newFee == null)
                throw new ArgumentNullException(nameof(newFee));

            MonthlyFeePerUnit = newFee;
        }

        public void UpdateEmergencyFund(Money newAmount)
        {
            if (newAmount == null)
                throw new ArgumentNullException(nameof(newAmount));

            EmergencyFund = newAmount;
        }

        public void ChangeChairperson(Guid newChairpersonId)
        {
            if (newChairpersonId == Guid.Empty)
                throw new ArgumentException("Chairperson ID cannot be empty", nameof(newChairpersonId));

            ChairpersonId = newChairpersonId;
            if (!_committeeMembers.Contains(newChairpersonId))
                _committeeMembers.Add(newChairpersonId);
        }

        public void ChangeTreasurer(Guid newTreasurerId)
        {
            if (newTreasurerId == Guid.Empty)
                throw new ArgumentException("Treasurer ID cannot be empty", nameof(newTreasurerId));

            TreasurerId = newTreasurerId;
            if (!_committeeMembers.Contains(newTreasurerId))
                _committeeMembers.Add(newTreasurerId);
        }

        public void AddCommitteeMember(Guid memberId)
        {
            if (memberId == Guid.Empty)
                throw new ArgumentException("Member ID cannot be empty", nameof(memberId));

            if (!_committeeMembers.Contains(memberId))
                _committeeMembers.Add(memberId);
        }

        public void RemoveCommitteeMember(Guid memberId)
        {
            if (memberId == ChairpersonId)
                throw new InvalidOperationException("Cannot remove the chairperson from the committee");

            if (memberId == TreasurerId)
                throw new InvalidOperationException("Cannot remove the treasurer from the committee");

            _committeeMembers.Remove(memberId);
        }

        public void AddBuildingProperty(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                throw new ArgumentException("Property ID cannot be empty", nameof(propertyId));

            if (!_buildingPropertyIds.Contains(propertyId))
                _buildingPropertyIds.Add(propertyId);
        }

        public void RemoveBuildingProperty(Guid propertyId)
        {
            _buildingPropertyIds.Remove(propertyId);
        }
    }
}
