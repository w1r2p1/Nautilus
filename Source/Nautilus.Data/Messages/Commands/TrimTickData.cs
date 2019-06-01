﻿// -------------------------------------------------------------------------------------------------
// <copyright file="TrimTickData.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Data.Messages.Commands
{
    using System;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using NodaTime;

    /// <summary>
    /// Represents a command to trim the tick data with timestamps prior to the trim from date time.
    /// </summary>
    [Immutable]
    public sealed class TrimTickData : Command, IScheduledJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrimTickData"/> class.
        /// </summary>
        /// <param name="trimFrom">The date time the tick data should be trimmed from.</param>
        /// <param name="scheduledTime">The commands scheduled time.</param>
        /// <param name="id">The commands identifier.</param>
        /// <param name="timestamp">The commands creation timestamp.</param>
        public TrimTickData(
            ZonedDateTime trimFrom,
            ZonedDateTime scheduledTime,
            Guid id,
            ZonedDateTime timestamp)
            : base(
                typeof(TrimBarData),
                id,
                timestamp)
        {
            Debug.NotDefault(trimFrom, nameof(trimFrom));
            Debug.NotDefault(scheduledTime, nameof(scheduledTime));

            this.ScheduledTime = scheduledTime;
            this.TrimFrom = trimFrom;
        }

        /// <inheritdoc />
        public ZonedDateTime ScheduledTime { get; }

        /// <summary>
        /// Gets the date time the tick data should be trimmed from.
        /// </summary>
        public ZonedDateTime TrimFrom { get; }
    }
}
