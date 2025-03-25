using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MaintainEase.Core.Domain.IsraeliMarket.Entities;
using MaintainEase.Core.Domain.IsraeliMarket.Interfaces;
using MaintainEase.Infrastructure.Data.Context;

namespace MaintainEase.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Jewish calendar data
    /// </summary>
    public class JewishCalendarRepository : BaseRepository<JewishCalendarData>, IJewishCalendarRepository
    {
        public JewishCalendarRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> IsHolidayOrShabbatAsync(DateTimeOffset date)
        {
            var calendarData = await _dbSet
                .FirstOrDefaultAsync(j => j.GregorianDate.Date == date.Date);
                
            return calendarData != null && (calendarData.IsHoliday || calendarData.IsShabbat);
        }

        public async Task<string> GetHolidayNameAsync(DateTimeOffset date)
        {
            var calendarData = await _dbSet
                .FirstOrDefaultAsync(j => j.GregorianDate.Date == date.Date);
                
            return calendarData?.Holiday;
        }

        public async Task<Dictionary<DateTimeOffset, string>> GetHolidaysInRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var holidays = await _dbSet
                .Where(j => j.GregorianDate.Date >= startDate.Date && 
                           j.GregorianDate.Date <= endDate.Date && 
                           j.IsHoliday)
                .Select(j => new { j.GregorianDate, j.Holiday })
                .ToListAsync();
                
            return holidays.ToDictionary(h => h.GregorianDate, h => h.Holiday);
        }

        public async Task<string> ConvertToHebrewDateAsync(DateTimeOffset gregorianDate)
        {
            var calendarData = await _dbSet
                .FirstOrDefaultAsync(j => j.GregorianDate.Date == gregorianDate.Date);
                
            if (calendarData == null)
                throw new InvalidOperationException($"Hebrew date not found for {gregorianDate.Date:d}");
                
            return calendarData.HebrewDate;
        }
    }
}
