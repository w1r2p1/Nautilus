﻿//--------------------------------------------------------------------------------------------------
// <copyright file="VersionChecker.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Common.Configuration
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using Microsoft.Extensions.Logging;
    using Nautilus.Core.Correctness;

    /// <summary>
    /// Provides a means of checking dependency versions and outputting to the log at service initialization.
    /// </summary>
    public static class VersionChecker
    {
        /// <summary>
        /// Runs the version checker which produces log events.
        /// </summary>
        /// <param name="logger">The logger for the version check.</param>
        /// <param name="serviceHeader">The service header string.</param>
        public static void Run(ILogger logger, string serviceHeader)
        {
            Condition.NotEmptyOrWhiteSpace(serviceHeader, nameof(serviceHeader));

            logger.LogInformation("=================================================================");
            logger.LogInformation(@"   _   _   ___   _   _  _____  _____  _      _   _  _____   ");
            logger.LogInformation(@"  | \ | | / _ \ | | | ||_   _||_   _|| |    | | | |/  ___|  ");
            logger.LogInformation(@"  |  \| |/ /_\ \| | | |  | |    | |  | |    | | | |\ `--.   ");
            logger.LogInformation(@"  | . ` ||  _  || | | |  | |    | |  | |    | | | | `--. \  ");
            logger.LogInformation(@"  | |\  || | | || |_| |  | |   _| |_ | |____| |_| |/\__/ /  ");
            logger.LogInformation(@"  \_| \_/\_| |_/ \___/   \_/   \___/ \_____/ \___/ \____/   ");
            logger.LogInformation("                                                             ");
            logger.LogInformation($" {serviceHeader}");
            logger.LogInformation(" by Nautech Systems Pty Ltd.");
            logger.LogInformation(" Copyright (C) 2015-2020 All rights reserved.");
            logger.LogInformation("=================================================================");
            logger.LogInformation(" SYSTEM SPECIFICATION");
            logger.LogInformation("=================================================================");
            logger.LogInformation($"CPU architecture: {RuntimeInformation.ProcessArchitecture}");
            logger.LogInformation($"CPU(s): {Environment.ProcessorCount}");
            logger.LogInformation($"RAM-Avail: {Math.Round((decimal)Environment.WorkingSet / 1000000, 2)} GB");
            logger.LogInformation($"OS: {Environment.OSVersion}");
            logger.LogInformation($"Is64BitOperatingSystem={Environment.Is64BitOperatingSystem}");
            logger.LogInformation($"Is64BitProcess={Environment.Is64BitProcess}");
            logger.LogInformation("=================================================================");
            logger.LogInformation(" VERSIONING");
            logger.LogInformation("=================================================================");
            logger.LogInformation($"{GetNetCoreVersion()}");
            logger.LogInformation($"Nautilus {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
            logger.LogInformation("=================================================================");
        }

        private static string GetNetCoreVersion()
        {
            return Assembly
                .GetEntryAssembly()?
                .GetCustomAttribute<TargetFrameworkAttribute>()?
                .FrameworkName ?? string.Empty;
        }
    }
}
