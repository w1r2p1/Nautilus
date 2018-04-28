﻿//--------------------------------------------------------------
// <copyright file="BarSpecification.cs" company="Nautech Systems Pty Ltd.">
//   Copyright (C) 2015-2017 Nautech Systems Pty Ltd. All rights reserved.
//   http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------

namespace Nautilus.DomainModel.ValueObjects
{
    using System;
    using System.Collections.Generic;
    using NautechSystems.CSharp.Annotations;
    using NautechSystems.CSharp.Validation;
    using Nautilus.DomainModel.Enums;
    using NodaTime;

    /// <summary>
    /// The immutable sealed <see cref="BarSpecification"/> class. Represents a bar profile being a time frame and
    /// period.
    /// </summary>
    [Immutable]
    public sealed class BarSpecification : ValueObject<BarSpecification>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarSpecification"/> class.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quoteType"></param>
        /// <param name="resolution">The bar time frame.</param>
        /// <param name="period">The bar period.</param>
        /// <exception cref="ValidationException">Throws if the period is zero or negative.</exception>
        public BarSpecification(
            Symbol symbol,
            BarQuoteType quoteType,
            BarResolution resolution,
            int period)
        {
            Validate.NotNull(symbol, nameof(symbol));
            Validate.Int32NotOutOfRange(period, nameof(period), 0, int.MaxValue, RangeEndPoints.Exclusive);

            this.Symbol = symbol;
            this.QuoteType = quoteType;
            this.Resolution = resolution;
            this.Period = period;
            this.TimePeriod = this.GetTimePeriod(period);
            this.Duration = this.TimePeriod.ToDuration();
        }

        /// <summary>
        /// Gets the bar specifications symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the bar specifications quote type.
        /// </summary>
        public BarQuoteType QuoteType { get; }

        /// <summary>
        /// Gets the bars specifications resolution.
        /// </summary>
        public BarResolution Resolution { get; }

        /// <summary>
        /// Gets the bars specifications period.
        /// </summary>
        public int Period { get;  }

        /// <summary>
        /// Gets the bars specifications time period.
        /// </summary>
        public Period TimePeriod { get; }

        /// <summary>
        /// Gets the duration.
        /// </summary>
        public Duration Duration { get; }

        /// <summary>
        /// Returns a value indicating whether this bars time period is one day.
        /// </summary>
        public bool IsOneDayBar => this.Resolution == BarResolution.Day && this.Period == 1;

        public static bool operator ==(BarSpecification left, BarSpecification right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BarSpecification left, BarSpecification right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns a value indicating whether this entity is equal to the given <see cref="object"/>.
        /// </summary>
        /// <param name="other">The other object.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public override bool Equals([CanBeNull] object other)
        {
            return other is BarSpecification barSpec && this.Equals(barSpec);
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="BarSpecification"/> is equal to the
        /// given <see cref="BarSpecification"/>.
        /// </summary>
        /// <param name="other">The other bar specification.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool Equals(BarSpecification other)
        {
            return this.Symbol.Equals(other.Symbol) &&
                   this.QuoteType.Equals(other.QuoteType) &&
                   this.Resolution.Equals(other.Resolution) &&
                   this.Period.Equals(other.Period);
        }

        /// <summary>
        /// Returns the hash code of the <see cref="BarSpecification"/>.
        /// </summary>
        /// <remarks>Non-readonly properties referenced in GetHashCode for serialization.</remarks>
        /// <returns>A <see cref="int"/>.</returns>
        public override int GetHashCode()
        {
            return this.Symbol.GetHashCode() +
                   this.QuoteType.GetHashCode() +
                   this.Resolution.GetHashCode() +
                   this.Period.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the <see cref="BarSpecification"/>.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString() => $"{this.Resolution}({this.Period})";

        /// <summary>
        /// Returns a collection of objects to be included in equality checks.
        /// </summary>
        /// <returns>A collection of objects.</returns>
        protected override IEnumerable<object> GetMembersForEqualityCheck()
        {
            return new object[]
                       {
                           this.ToString()
                       };
        }

        private Period GetTimePeriod(int barPeriod)
        {
            Debug.Int32NotOutOfRange(barPeriod, nameof(barPeriod), 0, int.MaxValue, RangeEndPoints.Exclusive);

            switch (this.Resolution)
            {
                case BarResolution.Tick:
                    return NodaTime.Period.Zero;

                case BarResolution.Second:
                    return NodaTime.Period.FromSeconds(barPeriod);

                case BarResolution.Minute:
                    return NodaTime.Period.FromMinutes(barPeriod);

                case BarResolution.Hour:
                    return NodaTime.Period.FromHours(barPeriod);

                case BarResolution.Day:
                    return NodaTime.Period.FromDays(barPeriod);

                case BarResolution.Week:
                    return NodaTime.Period.FromWeeks(barPeriod);

                case BarResolution.Month:
                    return NodaTime.Period.FromMonths(barPeriod);

                default: throw new InvalidOperationException("Bar resolution not recognised.");
            }
        }
    }
}
