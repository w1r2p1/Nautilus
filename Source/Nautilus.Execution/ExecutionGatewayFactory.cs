//--------------------------------------------------------------------------------------------------
// <copyright file="ExecutionGatewayFactory.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Execution
{
    using Nautilus.Common.Interfaces;
    using Nautilus.Core.Validation;

    /// <summary>
    /// Provides a factory for execution gateways.
    /// </summary>
    public class ExecutionGatewayFactory : IExecutionGatewayFactory
    {
        /// <summary>
        /// Creates and returns a new execution gateway.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="fixClient">The FIX client.</param>
        /// <param name="instrumentRepository">The instrument repository.</param>
        /// <returns>The execution gateway.</returns>
        public IExecutionGateway Create(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            IFixClient fixClient,
            IInstrumentRepository instrumentRepository)
        {
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));
            Validate.NotNull(fixClient, nameof(fixClient));
            Validate.NotNull(instrumentRepository, nameof(instrumentRepository));

            return new ExecutionGateway(
                container,
                messagingAdapter,
                fixClient,
                instrumentRepository);
        }
    }
}