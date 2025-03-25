using System;
using MaintainEase.Core.Domain.Entities;
using MaintainEase.Core.Domain.Enums;
using MaintainEase.Core.Domain.Events;
using MaintainEase.Core.Domain.IsraeliMarket.Enums;
using MaintainEase.Core.Domain.IsraeliMarket.ValueObjects;
using MaintainEase.Core.Domain.ValueObjects;

namespace MaintainEase.Core.Domain.IsraeliMarket.Entities
{
    /// <summary>
    /// Represents an Israeli property with specific attributes
    /// </summary>
    public class IsraeliProperty : Property
    {
        public IsraeliPropertyType IsraeliPropertyType { get; private set; }
        public TabuExtract TabuExtract { get; private set; }
        public ArnonaZone ArnonaZone { get; private set; }
        public bool IsKosher { get; private set; }
        public bool HasShabbatElevator { get; private set; }
        public bool HasSukkahBalcony { get; private set; }
        public bool IsVaadBayitMember { get; private set; }
        public decimal VaadBayitMonthlyFee { get; private set; }
        public string ArnonaBillingId { get; private set; }

        // For EF Core
        protected IsraeliProperty() { }

        public IsraeliProperty(
            string name,
            Address address,
            PropertyType type,
            DateTimeOffset acquisitionDate,
            Money purchasePrice,
            Money currentValue,
            int yearBuilt,
            decimal totalArea,
            string legalDescription,
            string taxIdentifier,
            IsraeliPropertyType israeliPropertyType,
            TabuExtract tabuExtract,
            ArnonaZone arnonaZone,
            bool isKosher = false,
            bool hasShabbatElevator = false,
            bool hasSukkahBalcony = false,
            bool isVaadBayitMember = false,
            decimal vaadBayitMonthlyFee = 0,
            string arnonaBillingId = null)
            : base(
                name,
                address,
                type,
                acquisitionDate,
                purchasePrice,
                currentValue,
                yearBuilt,
                totalArea,
                legalDescription,
                taxIdentifier)
        {
            IsraeliPropertyType = israeliPropertyType;
            TabuExtract = tabuExtract ?? throw new ArgumentNullException(nameof(tabuExtract));
            ArnonaZone = arnonaZone ?? throw new ArgumentNullException(nameof(arnonaZone));
            IsKosher = isKosher;
            HasShabbatElevator = hasShabbatElevator;
            HasSukkahBalcony = hasSukkahBalcony;
            IsVaadBayitMember = isVaadBayitMember;
            VaadBayitMonthlyFee = vaadBayitMonthlyFee;
            ArnonaBillingId = arnonaBillingId;
        }

        public void UpdateReligiousFeatures(
            bool isKosher,
            bool hasShabbatElevator,
            bool hasSukkahBalcony)
        {
            IsKosher = isKosher;
            HasShabbatElevator = hasShabbatElevator;
            HasSukkahBalcony = hasSukkahBalcony;
        }

        public void UpdateVaadBayitDetails(
            bool isVaadBayitMember,
            decimal vaadBayitMonthlyFee)
        {
            if (vaadBayitMonthlyFee < 0)
                throw new ArgumentException("Vaad Bayit monthly fee cannot be negative", nameof(vaadBayitMonthlyFee));

            IsVaadBayitMember = isVaadBayitMember;
            VaadBayitMonthlyFee = vaadBayitMonthlyFee;
        }

        public void UpdateTabuExtract(TabuExtract tabuExtract)
        {
            TabuExtract = tabuExtract ?? throw new ArgumentNullException(nameof(tabuExtract));
        }

        public void UpdateArnonaDetails(
            ArnonaZone arnonaZone,
            string arnonaBillingId)
        {
            ArnonaZone = arnonaZone ?? throw new ArgumentNullException(nameof(arnonaZone));
            ArnonaBillingId = arnonaBillingId;
        }
    }
}
