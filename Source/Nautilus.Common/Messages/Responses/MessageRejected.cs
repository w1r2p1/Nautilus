//--------------------------------------------------------------------------------------------------
// <copyright file="MessageRejected.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Messages.Responses
{
    using System;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using NodaTime;

    /// <summary>
    /// Represents a rejected message response.
    /// </summary>
    [Immutable]
    public sealed class MessageRejected : Response
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRejected"/> class.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="correlationId">The response correlation identifier.</param>
        /// <param name="id">The response identifier.</param>
        /// <param name="timestamp">The response timestamp.</param>
        public MessageRejected(
            string message,
            Guid correlationId,
            Guid id,
            ZonedDateTime timestamp)
            : base(
                typeof(MessageRejected),
                correlationId,
                id,
                timestamp)
        {
            Debug.NotDefault(id, nameof(id));
            Debug.NotDefault(timestamp, nameof(timestamp));

            this.Message = message;
        }

        /// <summary>
        /// Gets the responses message.
        /// </summary>
        public string Message { get; }
    }
}