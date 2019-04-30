﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Clock.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Componentry
{
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Validation;
    using NodaTime;

    /// <summary>
    /// The system clock with an imbedded timezone.
    /// </summary>
    public class Clock : IZonedClock
    {
        private readonly ZonedClock clock;
        private readonly DateTimeZone dateTimeZone;

        /// <summary>
        /// Initializes a new instance of the <see cref="Clock"/> class.
        /// </summary>
        /// <param name="dateTimeZone">The date time zone.</param>
        /// <exception cref="ValidationException">If the argument is null.</exception>
        public Clock(DateTimeZone dateTimeZone)
        {
            Precondition.NotNull(dateTimeZone, nameof(dateTimeZone));

            this.clock = new ZonedClock(SystemClock.Instance, dateTimeZone, CalendarSystem.Iso);
            this.dateTimeZone = dateTimeZone;
        }

        /// <summary>
        /// Returns the current time of this clock.
        /// </summary>
        /// <returns>A <see cref="ZonedDateTime"/>.</returns>
        public ZonedDateTime TimeNow() => this.clock.GetCurrentZonedDateTime();

        /// <summary>
        /// Returns the time zone of this clock.
        /// </summary>
        /// <returns>A <see cref="DateTimeZone"/>.</returns>
        public DateTimeZone GetTimeZone() => this.dateTimeZone;
    }
}
