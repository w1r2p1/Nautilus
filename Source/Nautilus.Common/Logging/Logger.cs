﻿//--------------------------------------------------------------------------------------------------
// <copyright file="Logger.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Logging
{
    using System;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.DomainModel.ValueObjects;

    /// <summary>
    /// Provides a logger with sends log events to the <see cref="ILoggingAdapter"/>.
    /// </summary>
    public sealed class Logger : ILogger
    {
        private readonly ILoggingAdapter loggingAdapter;
        private readonly NautilusService service;
        private readonly Label component;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="loggingAdapter">The logging adapter.</param>
        /// <param name="service">The service context.</param>
        /// <param name="component">The component name.</param>
        public Logger(
            ILoggingAdapter loggingAdapter,
            NautilusService service,
            Label component)
        {
            this.loggingAdapter = loggingAdapter;
            this.service = service;
            this.component = component;
        }

        /// <summary>
        /// Sends the given verbose message to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Verbose(string message)
        {
            this.loggingAdapter.Verbose(this.service, $"{this.component}: {message}");
        }

        /// <summary>
        /// Sends the given information message to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Information(string message)
        {
            this.loggingAdapter.Information(this.service, $"{this.component}: {message}");
        }

        /// <summary>
        /// Sends the given debug message to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Debug(string message)
        {
            this.loggingAdapter.Debug(this.service, $"{this.component}: {message}");
        }

        /// <summary>
        /// Sends the given warning message to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Warning(string message)
        {
            this.loggingAdapter.Warning(this.service, $"{this.component}: {message}");
        }

        /// <summary>
        /// Sends the given error message and exception to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Error(string message)
        {
            this.loggingAdapter.Error(this.service, $"{this.component}: {message}");
        }

        /// <summary>
        /// Sends the given error message and exception to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public void Error(string message, Exception ex)
        {
            this.loggingAdapter.Error(this.service, $"{this.component}: {message}", ex);
        }

        /// <summary>
        /// Sends the given fatal message and exception to the <see cref="ILoggingAdapter"/> to log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        public void Fatal(string message, Exception ex)
        {
            this.loggingAdapter.Fatal(this.service, $"{this.component}: {message}", ex);
        }
    }
}
