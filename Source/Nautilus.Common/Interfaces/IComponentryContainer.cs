﻿//--------------------------------------------------------------------------------------------------
// <copyright file="IComponentryContainer.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Interfaces
{
    public interface IComponentryContainer
    {
        /// <summary>
        /// Gets the containers clock.
        /// </summary>
        IZonedClock Clock { get; }

        /// <summary>
        /// Gets the containers guid factory.
        /// </summary>
        IGuidFactory GuidFactory { get; }

        /// <summary>
        /// Gets the containers logger.
        /// </summary>
        ILoggerFactory LoggerFactory { get; }
    }
}