﻿//--------------------------------------------------------------------------------------------------
// <copyright file="BarSpecification.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  https://nautechsystems.io
//
//  Licensed under the GNU Lesser General Public License Version 3.0 (the "License");
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at https://www.gnu.org/licenses/lgpl-3.0.en.html
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.DomainModel.ValueObjects
{
    using System;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using Nautilus.Core.Extensions;
    using Nautilus.DomainModel.Enums;
    using NodaTime;

    /// <summary>
    /// Represents a bar specification being a quote type, resolution and period.
    /// </summary>
    [Immutable]
    public readonly struct BarSpecification : IEquatable<object>, IEquatable<BarSpecification>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarSpecification"/> structure.
        /// </summary>
        /// <param name="period">The specification period.</param>
        /// <param name="barStructure">The specification resolution.</param>
        /// <param name="priceType">The specification quote type.</param>
        /// <exception cref="ArgumentOutOfRangeException">If the period is not positive (> 0).</exception>
        public BarSpecification(
            int period,
            BarStructure barStructure,
            PriceType priceType)
        {
            Debug.PositiveInt32(period, nameof(period));

            this.Period = period;
            this.BarStructure = barStructure;
            this.PriceType = priceType;
            this.Duration = CalculateDuration(period, barStructure);
        }

        /// <summary>
        /// Gets the bars specifications period.
        /// </summary>
        public int Period { get;  }

        /// <summary>
        /// Gets the bars specifications resolution.
        /// </summary>
        public BarStructure BarStructure { get; }

        /// <summary>
        /// Gets the bar specifications quote type.
        /// </summary>
        public PriceType PriceType { get; }

        /// <summary>
        /// Gets the bar time duration.
        /// </summary>
        public Duration Duration { get; }

        /// <summary>
        /// Returns a value indicating whether the <see cref="BarSpecification"/>s are equal.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public static bool operator ==(BarSpecification left, BarSpecification right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns a value indicating whether the <see cref="BarSpecification"/>s are not equal.
        /// </summary>
        /// <param name="left">The left object.</param>
        /// <param name="right">The right object.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public static bool operator !=(BarSpecification left,  BarSpecification right) => !(left == right);

        /// <summary>
        /// Returns a new <see cref="BarSpecification"/> from the given <see cref="string"/>.
        /// </summary>
        /// <param name="barSpecString">The bar specification string.</param>
        /// <returns>The created <see cref="BarSpecification"/>.</returns>
        public static BarSpecification FromString(string barSpecString)
        {
            Debug.NotEmptyOrWhiteSpace(barSpecString, nameof(barSpecString));

            var split1 = barSpecString.Split('-');
            var split2 = split1[1].Split('[');
            var period = Convert.ToInt32(split1[0]);
            var resolution = split2[0].ToUpper();
            var quoteType = split2[1].Trim(']').ToUpper();

            return new BarSpecification(
                period,
                resolution.ToEnum<BarStructure>(),
                quoteType.ToEnum<PriceType>());
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="BarSpecification"/> is equal
        /// to the given <see cref="object"/>.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public override bool Equals(object? other) => other is BarSpecification barSpec && this.Equals(barSpec);

        /// <summary>
        /// Returns a value indicating whether this <see cref="BarSpecification"/> is equal
        /// to the given <see cref="BarSpecification"/>.
        /// </summary>
        /// <param name="other">The other object.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool Equals(BarSpecification other)
        {
            return this.Period == other.Period &&
                   this.BarStructure == other.BarStructure &&
                   this.PriceType == other.PriceType;
        }

        /// <summary>
        /// Returns the hash code of the <see cref="BarSpecification"/>.
        /// </summary>
        /// <remarks>Non-readonly properties referenced in GetHashCode for serialization.</remarks>
        /// <returns>A <see cref="int"/>.</returns>
        public override int GetHashCode()
        {
            return Hash.GetCode(this.Period, this.BarStructure, this.PriceType);
        }

        /// <summary>
        /// Returns a string representation of the <see cref="BarSpecification"/>.
        /// </summary>
        /// <returns>A string.</returns>
        public override string ToString() => $"{this.Period}-" +
                                             $"{this.BarStructure.ToString().ToUpper()}" +
                                             $"[{this.PriceType.ToString().ToUpper()}]";

        private static Duration CalculateDuration(int barPeriod, BarStructure barStructure)
        {
            Debug.PositiveInt32(barPeriod, nameof(barPeriod));

            switch (barStructure)
            {
                case BarStructure.Tick:
                    return Duration.Zero;
                case BarStructure.Second:
                    return Duration.FromSeconds(barPeriod);
                case BarStructure.Minute:
                    return Duration.FromMinutes(barPeriod);
                case BarStructure.Hour:
                    return Duration.FromHours(barPeriod);
                case BarStructure.Day:
                    return Duration.FromDays(barPeriod);
                case BarStructure.Undefined:
                    goto default;
                default:
                    throw ExceptionFactory.InvalidSwitchArgument(barStructure, nameof(barStructure));
            }
        }
    }
}
