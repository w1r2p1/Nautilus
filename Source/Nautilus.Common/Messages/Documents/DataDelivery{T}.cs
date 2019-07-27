﻿// -------------------------------------------------------------------------------------------------
// <copyright file="DataDelivery{T}.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Messages.Documents
{
    using System;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using NodaTime;

    /// <summary>
    /// A delivery message of new data.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    [Immutable]
    public sealed class DataDelivery<T> : Document
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataDelivery{T}"/> class.
        /// </summary>
        /// <param name="data">The message data.</param>
        /// <param name="id">The message identifier.</param>
        /// <param name="timestamp">The message timestamp.</param>
        public DataDelivery(
            T data,
            Guid id,
            ZonedDateTime timestamp)
            : base(typeof(DataDelivery<T>), id, timestamp)
        {
            Debug.NotDefault(id, nameof(id));
            Debug.NotDefault(timestamp, nameof(timestamp));

            this.Data = data;
        }

        /// <summary>
        /// Gets the documents market data.
        /// </summary>
        public T Data { get; }
    }
}
