using System;
using MaintainEase.Core.Domain.Interfaces;

namespace MaintainEase.Infrastructure.Services
{
    /// <summary>
    /// Implementation of date time service
    /// </summary>
    public class DateTimeService : IDateTimeService
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
