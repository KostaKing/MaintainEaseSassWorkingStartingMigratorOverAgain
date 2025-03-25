using System;
using MaintainEase.Core.Domain.Entities;

namespace MaintainEase.Core.Domain.IsraeliMarket.Entities
{
    /// <summary>
    /// Represents Jewish calendar data for a specific date
    /// </summary>
    public class JewishCalendarData : Entity
    {
        public DateTimeOffset GregorianDate { get; private set; }
        public string HebrewDate { get; private set; }
        public string Holiday { get; private set; }
        public bool IsHoliday { get; private set; }
        public bool IsShabbat { get; private set; }
        public bool IsFastDay { get; private set; }
        public string ParashaName { get; private set; }

        // For EF Core
        protected JewishCalendarData() { }

        public JewishCalendarData(
            DateTimeOffset gregorianDate,
            string hebrewDate,
            string holiday,
            bool isHoliday,
            bool isShabbat,
            bool isFastDay,
            string parashaName)
        {
            if (string.IsNullOrWhiteSpace(hebrewDate))
                throw new ArgumentException("Hebrew date cannot be empty", nameof(hebrewDate));

            GregorianDate = gregorianDate;
            HebrewDate = hebrewDate;
            Holiday = holiday;
            IsHoliday = isHoliday;
            IsShabbat = isShabbat;
            IsFastDay = isFastDay;
            ParashaName = parashaName;
        }
    }
}
