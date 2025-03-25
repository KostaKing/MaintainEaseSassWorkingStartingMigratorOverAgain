using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaintainEase.Core.Domain.Interfaces;
using MaintainEase.Core.Domain.IsraeliMarket.Interfaces;

namespace MaintainEase.Core.Domain.IsraeliMarket.Services
{
    /// <summary>
    /// Service for scheduling maintenance and payments considering Jewish holidays
    /// </summary>
    public class HolidayAwareSchedulingService : IDomainService
    {
        private readonly IJewishCalendarRepository _jewishCalendarRepository;

        public HolidayAwareSchedulingService(IJewishCalendarRepository jewishCalendarRepository)
        {
            _jewishCalendarRepository = jewishCalendarRepository ?? throw new ArgumentNullException(nameof(jewishCalendarRepository));
        }

        public async Task<DateTimeOffset> GetNextValidMaintenanceDate(DateTimeOffset requestedDate)
        {
            // Check if the requested date falls on a holiday or Shabbat
            if (await _jewishCalendarRepository.IsHolidayOrShabbatAsync(requestedDate))
            {
                // Find the next available date that is not a holiday or Shabbat
                DateTimeOffset nextDate = requestedDate.AddDays(1);
                while (await _jewishCalendarRepository.IsHolidayOrShabbatAsync(nextDate))
                {
                    nextDate = nextDate.AddDays(1);
                }
                return nextDate;
            }

            return requestedDate;
        }

        public async Task<DateTimeOffset> GetNextValidPaymentDate(DateTimeOffset dueDate)
        {
            // If due date falls on a holiday or Shabbat, move to the next business day
            if (await _jewishCalendarRepository.IsHolidayOrShabbatAsync(dueDate))
            {
                // Find the next available date that is not a holiday or Shabbat
                DateTimeOffset nextDate = dueDate.AddDays(1);
                while (await _jewishCalendarRepository.IsHolidayOrShabbatAsync(nextDate))
                {
                    nextDate = nextDate.AddDays(1);
                }
                return nextDate;
            }

            return dueDate;
        }

        public async Task<IEnumerable<DateTimeOffset>> GenerateMaintenanceSchedule(
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            int intervalDays)
        {
            var schedule = new List<DateTimeOffset>();
            DateTimeOffset currentDate = startDate;

            while (currentDate <= endDate)
            {
                // Add only valid dates that don't fall on holidays or Shabbat
                if (!await _jewishCalendarRepository.IsHolidayOrShabbatAsync(currentDate))
                {
                    schedule.Add(currentDate);
                }
                
                // Move to the next interval
                currentDate = currentDate.AddDays(intervalDays);
            }

            return schedule;
        }

        public async Task<bool> CanScheduleMaintenance(DateTimeOffset requestedDate, bool isEmergency)
        {
            // Emergency maintenance can be scheduled on any day
            if (isEmergency)
                return true;

            // Regular maintenance cannot be scheduled on holidays or Shabbat
            return !await _jewishCalendarRepository.IsHolidayOrShabbatAsync(requestedDate);
        }
    }
}
