﻿// -------------------------------------------------------------------------------------------------
// <copyright file="MarketDataPersisted.cs" company="Nautech Systems Pty Ltd.">
//   Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Database.Core.Messages.Events
{
    using System;
    using NautechSystems.CSharp.Annotations;
    using NautechSystems.CSharp.Validation;
    using NodaTime;
    using Nautilus.Core;
    using Nautilus.DomainModel.ValueObjects;

    [Immutable]
    public sealed class MarketDataPersisted : Event
    {
        public MarketDataPersisted(
            BarSpecification barSpec,
            ZonedDateTime lastBarTime,
            Guid identifier,
            ZonedDateTime timestamp)
            : base(identifier, timestamp)
        {
            Validate.NotNull(barSpec, nameof(barSpec));
            Validate.NotDefault(lastBarTime, nameof(lastBarTime));
            Validate.NotDefault(identifier, nameof(identifier));
            Validate.NotDefault(timestamp, nameof(timestamp));

            this.BarSpecification = barSpec;
            this.LastBarTime = lastBarTime;
        }

        public BarSpecification BarSpecification { get; }

        public ZonedDateTime LastBarTime { get; }

        /// <summary>
        /// Gets a string representation of the <see cref="MarketDataPersisted"/> message.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => $"{nameof(MarketDataPersisted)}-{this.Id}";
    }
}
