﻿//--------------------------------------------------------------------------------------------------
// <copyright file="StubZonedDateTime.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  https://nautechsystems.io
//
//  Licensed under the GNU Lesser General Public License Version 3.0 (the "License");
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at https://www.gnu.org/licenses/lgpl-3.0.en.html
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.Stubs
{
    using System.Diagnostics.CodeAnalysis;
    using NodaTime;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public static class StubZonedDateTime
    {
        private static readonly ZonedDateTime UnixEpochZonedDateTime = new ZonedDateTime(Instant.FromUnixTimeSeconds(0), DateTimeZone.Utc);

        public static ZonedDateTime UnixEpoch()
        {
            return UnixEpochZonedDateTime;
        }
    }
}
