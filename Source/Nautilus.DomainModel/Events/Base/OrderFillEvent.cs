﻿//--------------------------------------------------------------------------------------------------
// <copyright file="OrderFillEvent.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.DomainModel.Events.Base
{
    using System;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;

    /// <summary>
    /// The base class for all order fill events.
    /// </summary>
    [Immutable]
    public abstract class OrderFillEvent : OrderEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFillEvent"/> class.
        /// </summary>
        /// <param name="accountId">The event account identifier.</param>
        /// <param name="orderId">The event order identifier.</param>
        /// <param name="executionId">The event order execution identifier.</param>
        /// <param name="positionIdBroker">The event broker position identifier.</param>
        /// <param name="symbol">The event order symbol.</param>
        /// <param name="orderSide">The event order side.</param>
        /// <param name="filledQuantity">The event order filled quantity.</param>
        /// <param name="averagePrice">The event order average price.</param>
        /// <param name="currency">The event transaction currency.</param>
        /// <param name="executionTime">The event order execution time.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="eventTimestamp">The event timestamp.</param>
        protected OrderFillEvent(
            AccountId accountId,
            OrderId orderId,
            ExecutionId executionId,
            PositionIdBroker positionIdBroker,
            Symbol symbol,
            OrderSide orderSide,
            Quantity filledQuantity,
            Price averagePrice,
            Currency currency,
            ZonedDateTime executionTime,
            Type eventType,
            Guid eventId,
            ZonedDateTime eventTimestamp)
            : base(
                orderId,
                eventType,
                eventId,
                eventTimestamp)
        {
            Debug.NotDefault(orderSide, nameof(orderSide));
            Debug.NotDefault(executionTime, nameof(executionTime));
            Debug.NotDefault(eventId, nameof(eventId));
            Debug.NotDefault(eventTimestamp, nameof(eventTimestamp));

            this.AccountId = accountId;
            this.PositionIdBroker = positionIdBroker;
            this.ExecutionId = executionId;
            this.Symbol = symbol;
            this.OrderSide = orderSide;
            this.FilledQuantity = filledQuantity;
            this.AveragePrice = averagePrice;
            this.Currency = currency;
            this.ExecutionTime = executionTime;
        }

        /// <summary>
        /// Gets the events account identifier.
        /// </summary>
        public AccountId AccountId { get; }

        /// <summary>
        /// Gets the events broker position identifier.
        /// </summary>
        public PositionIdBroker PositionIdBroker { get; }

        /// <summary>
        /// Gets the events order execution identifier.
        /// </summary>
        public ExecutionId ExecutionId { get; }

        /// <summary>
        /// Gets the events order symbol.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the events order side.
        /// </summary>
        public OrderSide OrderSide { get; }

        /// <summary>
        /// Gets the events order filled quantity.
        /// </summary>
        public Quantity FilledQuantity { get; }

        /// <summary>
        /// Gets the events order average filled price.
        /// </summary>
        public Price AveragePrice { get; }

        /// <summary>
        /// Gets the events order transaction currency.
        /// </summary>
        public Currency Currency { get; }

        /// <summary>
        /// Gets the events order execution time.
        /// </summary>
        public ZonedDateTime ExecutionTime { get; }
    }
}
