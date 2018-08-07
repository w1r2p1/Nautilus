﻿//--------------------------------------------------------------------------------------------------
// <copyright file="SignalLogic.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.BlackBox.AlphaModel.Strategy
{
    using System.Collections.Generic;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.BlackBox.Core.Interfaces;
    using Nautilus.DomainModel.Entities;

    /// <summary>
    /// The immutable sealed <see cref="SignalLogic"/> class. Determines whether entry signals are
    /// valid based on the given inputs.
    /// </summary>
    [Immutable]
    public sealed class SignalLogic : ISignalLogic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalLogic"/> class.
        /// </summary>
        /// <param name="oppositeDirectionSignalBlocksEntries">The opposite direction signal blocks entries boolean.</param>
        /// <param name="sameDirectionExitSignalBlocksEntries">The exits block entries boolean.</param>
        public SignalLogic(
            bool oppositeDirectionSignalBlocksEntries,
            bool sameDirectionExitSignalBlocksEntries)
        {
            this.OppositeDirectionSignalBlocksEntries = oppositeDirectionSignalBlocksEntries;
            this.SameDirectionExitSignalBlocksEntries = sameDirectionExitSignalBlocksEntries;
        }

        /// <summary>
        /// Gets a value indicating whether opposite direction signal blocks entries.
        /// </summary>
        public bool OppositeDirectionSignalBlocksEntries { get; }

        /// <summary>
        /// Gets a value indicating whether exits block entries.
        /// </summary>
        public bool SameDirectionExitSignalBlocksEntries { get; }

        /// <summary>
        /// Returns a value indicating whether a buy signal is valid with the given inputs.
        /// </summary>
        /// <param name="entrySignalsBuy">The buy entry signals.</param>
        /// <param name="entrySignalsSell">The sell entry signals.</param>
        /// <param name="exitSignalLong">The long exit signal (optional).</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool IsValidBuySignal(
            IReadOnlyCollection<EntrySignal> entrySignalsBuy,
            IReadOnlyCollection<EntrySignal> entrySignalsSell,
            Option<ExitSignal> exitSignalLong)
        {
            Validate.NotNull(entrySignalsBuy, nameof(entrySignalsBuy));
            Validate.NotNull(entrySignalsSell, nameof(entrySignalsSell));
            Validate.NotNull(exitSignalLong, nameof(exitSignalLong));

            if (entrySignalsBuy.Count == 0)
            {
                return false;
            }

            if (this.OppositeDirectionSignalBlocksEntries && entrySignalsSell.Count > 0)
            {
                return false;
            }

            if (this.SameDirectionExitSignalBlocksEntries && exitSignalLong.HasValue)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a value indicating whether a sell signal is valid with the given inputs.
        /// </summary>
        /// <param name="entrySignalsBuy">The buy entry signals.</param>
        /// <param name="entrySignalsSell">The sell entry signals.</param>
        /// <param name="exitSignalShort">The short exit signal (optional).</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool IsValidSellSignal(
            IReadOnlyCollection<EntrySignal> entrySignalsBuy,
            IReadOnlyCollection<EntrySignal> entrySignalsSell,
            Option<ExitSignal> exitSignalShort)
        {
            Validate.NotNull(entrySignalsBuy, nameof(entrySignalsBuy));
            Validate.NotNull(entrySignalsSell, nameof(entrySignalsSell));
            Validate.NotNull(exitSignalShort, nameof(exitSignalShort));

            if (entrySignalsSell.Count == 0)
            {
                return false;
            }

            if (this.OppositeDirectionSignalBlocksEntries && entrySignalsBuy.Count > 0)
            {
                return false;
            }

            if (this.SameDirectionExitSignalBlocksEntries && exitSignalShort.HasValue)
            {
                return false;
            }

            return true;
        }
    }
}
