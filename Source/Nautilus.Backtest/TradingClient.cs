﻿//--------------------------------------------------------------------------------------------------
// <copyright file="BrokerageClient.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Backtest
{
    using System;
    using System.Collections.Generic;
    using Nautilus.Common.Interfaces;
    using Nautilus.DomainModel.Aggregates;
    using Nautilus.DomainModel.Entities;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.ValueObjects;

    /// <summary>
    /// The brokerage client.
    /// </summary>
    public class TradingClient : ITradingClient
    {
        /// <summary>
        ///
        /// </summary>
        public Broker Broker { get; } = Broker.Simulation;

        /// <summary>
        ///
        /// </summary>
        public bool IsConnected => true;

        /// <summary>
        ///
        /// </summary>
        /// <param name="gateway"></param>
        public void InitializeBrokerageGateway(IBrokerageGateway gateway)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public void Connect()
        {
            Console.WriteLine("Connecting to simulated brokerage...");
        }

        /// <summary>
        ///
        /// </summary>
        public void Disconnect()
        {
            Console.WriteLine("Disconnecting from simulated brokerage...");
        }

        /// <summary>
        ///
        /// </summary>
        public void InitializeSession()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="symbol"></param>
        public void RequestMarketDataSubscribe(Symbol symbol)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="symbol"></param>
        public void UpdateInstrumentSubscribe(Symbol symbol)
        {
        }

        /// <summary>
        ///
        /// </summary>
        public void UpdateInstrumentsSubscribeAll()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="atomicOrder"></param>
        public void SubmitEntryLimitStopOrder(AtomicOrder atomicOrder)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="atomicOrder"></param>
        public void SubmitEntryStopOrder(AtomicOrder atomicOrder)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stoplossModification"></param>
        public void ModifyStoplossOrder(KeyValuePair<Order, Price> stoplossModification)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="order"></param>
        public void CancelOrder(Order order)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="position"></param>
        public void ClosePosition(Position position)
        {
        }
    }
}