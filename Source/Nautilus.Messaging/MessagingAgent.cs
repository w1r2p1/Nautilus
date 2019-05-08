// -------------------------------------------------------------------------------------------------
// <copyright file="MessagingAgent.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Messaging
{
    using System;
    using System.Collections.Generic;
    using Nautilus.Messaging.Internal;

    /// <summary>
    /// The abstract base class for all messaging agents.
    /// </summary>
    public abstract class MessagingAgent
    {
        private readonly MessageProcessor processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingAgent"/> class.
        /// </summary>
        protected MessagingAgent()
        {
            this.processor = new MessageProcessor();
        }

        /// <summary>
        /// Gets the agents end point.
        /// </summary>
        public Endpoint Endpoint => this.processor.Endpoint;

        /// <summary>
        /// Gets the message input count for the processor.
        /// </summary>
        public int InputCount => this.processor.InputCount;

        /// <summary>
        /// Gets the message handler types.
        /// </summary>
        public IEnumerable<Type> HandlerTypes => this.processor.HandlerTypes;

        /// <summary>
        /// Gets the unhandled messages.
        /// </summary>
        public IEnumerable<object> UnhandledMessages => this.processor.UnhandledMessages;

        /// <summary>
        /// Register the given message type with the given handler.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="handler">The handler.</param>
        public void RegisterHandler<TMessage>(Action<TMessage> handler)
        {
            this.processor.RegisterHandler(handler);
        }

        /// <summary>
        /// Register the given handler to receive unhandled messaged.
        /// </summary>ve
        /// <param name="handler">The handler.</param>
        public void RegisterUnhandled(Action<object> handler)
        {
            this.processor.RegisterUnhandled(handler);
        }

        /// <summary>
        /// Send the given message to this agents endpoint.
        /// </summary>
        /// <param name="message">The message to send.</param>
        protected void SendToSelf(object message)
        {
            this.Endpoint.Send(message);
        }
    }
}