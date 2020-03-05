﻿//--------------------------------------------------------------------------------------------------
// <copyright file="ExecutionService.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Execution
{
    using System;
    using System.Collections.Generic;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Common.Messaging;
    using Nautilus.Messaging;
    using Nautilus.Scheduling;
    using Nautilus.Service;

    /// <summary>
    /// Provides an execution service.
    /// </summary>
    public sealed class ExecutionService : NautilusServiceBase
    {
        private readonly ITradingGateway tradingGateway;
        private readonly List<Address> managedComponents;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionService"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="messageBusAdapter">The messaging adapter.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="tradingGateway">The execution gateway.</param>
        /// <param name="config">The execution service configuration.</param>
        /// <exception cref="ArgumentException">If the addresses is empty.</exception>
        public ExecutionService(
            IComponentryContainer container,
            MessageBusAdapter messageBusAdapter,
            IScheduler scheduler,
            ITradingGateway tradingGateway,
            ServiceConfiguration config)
            : base(
                container,
                messageBusAdapter,
                scheduler,
                config.FixConfig)
        {
            this.tradingGateway = tradingGateway;

            this.managedComponents = new List<Address>
            {
                ServiceAddress.CommandServer,
                ServiceAddress.EventPublisher,
            };

            this.RegisterConnectionAddress(ServiceAddress.TradingGateway);
        }

        /// <inheritdoc />
        protected override void OnServiceStart(Start start)
        {
            // Forward start message
            this.Send(start, this.managedComponents);
        }

        /// <inheritdoc />
        protected override void OnServiceStop(Stop stop)
        {
            // Forward stop message
            this.Send(stop, this.managedComponents);
        }

        /// <inheritdoc />
        protected override void OnConnected()
        {
            this.tradingGateway.AccountInquiry();
            this.tradingGateway.SubscribeToPositionEvents();
        }
    }
}
