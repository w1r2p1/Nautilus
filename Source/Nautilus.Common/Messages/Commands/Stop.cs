﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Stop.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Messages.Commands
{
    using System;
    using Nautilus.Common.Messages.Commands.Base;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Correctness;
    using NodaTime;

    /// <summary>
    /// Represents a command to stop the component.
    /// </summary>
    [Immutable]
    public sealed class Stop : SystemCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Stop"/> class.
        /// </summary>
        /// <param name="messageId">The commands identifier.</param>
        /// <param name="messageTimestamp">The commands timestamp.</param>
        public Stop(
            Guid messageId,
            ZonedDateTime messageTimestamp)
            : base(messageId, messageTimestamp)
        {
            Debug.NotDefault(messageId, nameof(messageId));
            Debug.NotDefault(messageTimestamp, nameof(messageTimestamp));
        }
    }
}