﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Quantity.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2017 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.DomainModel.ValueObjects
{
    using System;
    using System.Collections.Generic;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;

    /// <summary>
    /// Represents a none-negative quantity.
    /// </summary>
    [Immutable]
    public sealed class Quantity
        : IntegerNumber<Quantity>, IEquatable<Quantity>, IComparable<IntegerNumber<Quantity>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Quantity"/> class.
        /// </summary>
        /// <param name="amount">The quantity amount.</param>
        private Quantity(int amount)
            : base(amount)
        {
            Debug.Int32NotOutOfRange(amount, nameof(amount), 0, int.MaxValue);
        }

        /// <summary>
        /// Returns a new <see cref="Quantity"/> with zero value.
        /// </summary>
        /// <returns>A <see cref="Quantity"/>.</returns>
        public static Quantity Zero()
        {
            return new Quantity(0);
        }

        /// <summary>
        /// Returns a new <see cref="Quantity"/> with the given value amount.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>A <see cref="Quantity"/>.</returns>
        public static Quantity Create(int amount)
        {
            return new Quantity(amount);
        }

        /// <summary>
        /// Returns a new <see cref="Quantity"/> as the result of the sum of this <see cref="Quantity"/>
        /// and the given <see cref="Quantity"/>.
        /// </summary>
        /// <param name="other">The other quantity.</param>
        /// <returns>A <see cref="Quantity"/>.</returns>
        public Quantity Add(Quantity other)
        {
            Debug.NotNull(other, nameof(other));

            return new Quantity(this.Value + other.Value);
        }

        /// <summary>
        /// Returns a new <see cref="Quantity"/> as the result of the given <see cref="Quantity"/>
        /// subtracted from this <see cref="Quantity"/> (cannot return a <see cref="Quantity"/>
        /// with a negative value).
        /// </summary>
        /// <param name="other">The other quantity.</param>
        /// <returns>A <see cref="Quantity"/>.</returns>
        public Quantity Subtract(Quantity other)
        {
            Debug.NotNull(other, nameof(other));
            Debug.True(other.Value <= this.Value, nameof(other));

            return new Quantity(this.Value - other.Value);
        }

        /// <summary>
        /// Returns a new <see cref="Quantity"/> as the result of this <see cref="Quantity"/>
        /// multiplied by the given multiple.
        /// </summary>
        /// <param name="multiplier">The multiplier.</param>
        /// <returns>A <see cref="Quantity"/>.</returns>
        public Quantity MultiplyBy(int multiplier)
        {
            Debug.DecimalNotOutOfRange(multiplier, nameof(multiplier), decimal.Zero, decimal.MaxValue);

            return new Quantity(this.Value * multiplier);
        }

        /// <summary>
        /// Returns a string representation of this <see cref="Quantity"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString() => this.Value.ToString();

        /// <summary>
        /// Returns a collection of objects to be included in equality checks.
        /// </summary>
        /// <returns>
        /// A collection of objects.
        /// </returns>
        protected override IEnumerable<object> GetMembersForEqualityCheck()
        {
            return new object[] { this.Value };
        }
    }
}
