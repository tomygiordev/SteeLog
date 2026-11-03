using System;
using SteeLog.Core.Application.Common;
namespace SteeLog.Infrastructure.Persistence
{
    public sealed class BusinessClock : IClock
    {
        private readonly TimeZoneInfo zona;
        private readonly Func<DateTimeOffset> utc;
        public BusinessClock(TimeZoneInfo zona, Func<DateTimeOffset> utc)
        {
            this.zona = zona ?? throw new ArgumentNullException(nameof(zona));
            this.utc = utc ?? throw new ArgumentNullException(nameof(utc));
        }
        public DateTime FechaNegocio => TimeZoneInfo.ConvertTime(utc().ToUniversalTime(), zona).Date;
    }
}
