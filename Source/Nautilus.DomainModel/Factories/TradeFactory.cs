﻿//--------------------------------------------------------------------------------------------------
// <copyright file="TradeFactory.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.DomainModel.Factories
{
    using System.Collections.Generic;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Collections;
    using Nautilus.DomainModel.Aggregates;
    using Nautilus.DomainModel.Entities;

    /// <summary>
    /// A factory which creates valid <see cref="Trade"/>(s) for the system.
    /// </summary>
    [Immutable]
    public static class TradeFactory
    {
        /// <summary>
        /// Creates and returns a new <see cref="Trade"/> from the given <see cref="AtomicOrdersPacket"/>.
        /// </summary>
        /// <param name="orderPacket">The order packet.</param>
        /// <returns>A <see cref="Trade"/>.</returns>
        public static Trade Create(AtomicOrdersPacket orderPacket)
        {
            var tradeUnits = CreateTradeUnits(orderPacket);

            return new Trade(
                orderPacket.Symbol,
                orderPacket.Id,
                orderPacket.TradeType,
                tradeUnits,
                orderPacket.OrderIdList,
                orderPacket.Timestamp);
        }

        private static ReadOnlyList<TradeUnit> CreateTradeUnits(AtomicOrdersPacket orderPacket)
        {
            var tradeId = orderPacket.Id;
            var tradeUnits = new List<TradeUnit>();

            foreach (var atomicOrder in orderPacket.Orders)
            {
                tradeUnits.Add(new TradeUnit(
                    EntityIdFactory.TradeUnit(tradeId, tradeUnits.Count),
                    LabelFactory.TradeUnit(tradeUnits.Count),
                    atomicOrder.EntryOrder,
                    atomicOrder.StopLossOrder,
                    atomicOrder.ProfitTargetOrder,
                    orderPacket.Timestamp));
            }

            return new ReadOnlyList<TradeUnit>(tradeUnits);
        }
    }
}
