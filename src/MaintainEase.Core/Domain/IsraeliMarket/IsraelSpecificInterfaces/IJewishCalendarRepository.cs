using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;

namespace MaintainEase.Core.Domain.IsraeliMarket.Interfaces
{
    /// <summary>
    /// Repository interface for Jewish calendar and holiday data
    /// </summary>
    public interface IJewishCalendarRepository : IRepository<JewishCalendarData>
    {
        /// <summary>
        /// Checks if a date falls on a Jewish holiday or Shabbat
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>True if the date is a holiday or Shabbat, false otherwise</returns>
        Task<bool> IsHolidayOrShabbatAsync(DateTimeOffset date);

        /// <summary>
        /// Gets the name of the holiday on a given date, if any
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>The name of the holiday, or null if not a holiday</returns>
        Task<string> GetHolidayNameAsync(DateTimeOffset date);

        /// <summary>
        /// Gets all holidays between two dates
        /// </summary>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <returns>A dictionary of dates and holiday names</returns>
        Task<Dictionary<DateTimeOffset, string>> GetHolidaysInRangeAsync(
            DateTimeOffset startDate,
            DateTimeOffset endDate);

        /// <summary>
        /// Converts a Gregorian date to a Hebrew date
        /// </summary>
        /// <param name="gregorianDate">The Gregorian date</param>
        /// <returns>A string representation of the Hebrew date</returns>
        Task<string> ConvertToHebrewDateAsync(DateTimeOffset gregorianDate);
    }
}
