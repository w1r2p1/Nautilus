﻿// -------------------------------------------------------------------------------------------------
// <copyright file="KeyProvider.cs" company="Nautech Systems Pty Ltd">
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
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Redis.Data.Internal
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Nautilus.Core.Correctness;
    using Nautilus.Core.Extensions;
    using Nautilus.Data.Keys;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;
    using StackExchange.Redis;

    /// <summary>
    /// Provides database keys for market data.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Allows nameof.")]
    public static class KeyProvider
    {
        private const string NautilusData = nameof(NautilusData) + ":" + nameof(Nautilus.Data);
        private const string Ticks = nameof(Ticks);
        private const string Bars = nameof(Bars);
        private const string Instruments = nameof(Instruments);

        private static readonly string TicksNamespace = $"{NautilusData}:{Ticks}";
        private static readonly string BarsNamespace = $"{NautilusData}:{Bars}";
        private static readonly string InstrumentsNamespace = $"{NautilusData}:{Instruments}";

        /// <summary>
        /// Returns an array of <see cref="DateKey"/>s based on the given from and to <see cref="ZonedDateTime"/> range.
        /// </summary>
        /// <param name="fromDateTime">The from date time.</param>
        /// <param name="toDateTime">The to date time.</param>
        /// <returns>An array of <see cref="DateKey"/>.</returns>
        internal static List<DateKey> GetDateKeys(ZonedDateTime fromDateTime, ZonedDateTime toDateTime)
        {
            Debug.True(fromDateTime.IsLessThanOrEqualTo(toDateTime), "fromDateTime.IsLessThanOrEqualTo(toDateTime)");

            return GetDateKeys(new DateKey(fromDateTime), new DateKey(toDateTime));
        }

        /// <summary>
        /// Returns an array of <see cref="DateKey"/>s based on the given from and to <see cref="ZonedDateTime"/> range.
        /// </summary>
        /// <param name="fromDate">The from date.</param>
        /// <param name="toDate">The to date.</param>
        /// <returns>An array of <see cref="DateKey"/>.</returns>
        internal static List<DateKey> GetDateKeys(DateKey fromDate, DateKey toDate)
        {
            Debug.True(fromDate.CompareTo(toDate) <= 0, "fromDate.CompareTo(toDate) <= 0");

            var difference = (int)((toDate.StartOfDay - fromDate.StartOfDay) / Duration.FromDays(1));
            var dateRange = new List<DateKey> { fromDate };
            for (var i = 0; i < difference; i++)
            {
                dateRange.Add(new DateKey(fromDate.DateUtc.Date + Period.FromDays(i + 1)));
            }

            return dateRange;
        }

        /// <summary>
        /// Returns a tick data namespace wildcard string.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        internal static RedisValue GetTicksPattern()
        {
            return TicksNamespace + "*";
        }

        /// <summary>
        /// Returns a tick data namespace wildcard string from the given symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>A <see cref="string"/>.</returns>
        internal static RedisValue GetTicksPattern(Symbol symbol)
        {
            return $"{TicksNamespace}:{symbol.Venue.Value}:{symbol.Code}*";
        }

        /// <summary>
        /// Returns the tick data key for the given parameters.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="dateKey">The date key.</param>
        /// <returns>The key string.</returns>
        internal static RedisKey GetTicksKey(Symbol symbol, DateKey dateKey)
        {
            return $"{TicksNamespace}:{symbol.Venue.Value}:{symbol.Code}:{dateKey}";
        }

        /// <summary>
        /// Returns an array of <see cref="DateKey"/>s based on the given from and to
        /// <see cref="ZonedDateTime"/> range.
        /// </summary>
        /// <param name="symbol">The ticks symbol.</param>
        /// <param name="fromDate">The ticks from date time.</param>
        /// <param name="toDate">The ticks to date time.</param>
        /// <returns>An array of <see cref="DateKey"/>.</returns>
        /// <remarks>The given time range should have been previously validated.</remarks>
        internal static RedisKey[] GetTicksKeys(Symbol symbol, DateKey fromDate, DateKey toDate)
        {
            return GetDateKeys(fromDate, toDate)
                .Select(key => GetTicksKey(symbol, key))
                .ToArray();
        }

        /// <summary>
        /// Returns a wildcard key string for all bars.
        /// </summary>
        /// <returns>The key <see cref="string"/>.</returns>
        internal static RedisValue GetBarsPattern()
        {
            return BarsNamespace + "*";
        }

        /// <summary>
        /// Returns a wildcard string from the given symbol bar spec.
        /// </summary>
        /// <param name="barType">The symbol bar spec.</param>
        /// <returns>A <see cref="string"/>.</returns>
        internal static RedisValue GetBarsPattern(BarType barType)
        {
            return BarsNamespace +
                   $":{barType.Symbol.Venue.Value}" +
                   $":{barType.Symbol.Code}" +
                   $":{barType.Specification.BarStructure}" +
                   $":{barType.Specification.PriceType}*";
        }

        /// <summary>
        /// Returns the bar data key for the given parameters.
        /// </summary>
        /// <param name="barType">The bar type.</param>
        /// <param name="dateKey">The date key.</param>
        /// <returns>The key <see cref="string"/>.</returns>
        internal static RedisKey GetBarsKey(BarType barType, DateKey dateKey)
        {
            return BarsNamespace +
                   $":{barType.Symbol.Venue.Value}" +
                   $":{barType.Symbol.Code}" +
                   $":{barType.Specification.BarStructure}" +
                   $":{barType.Specification.PriceType}" +
                   $":{dateKey}";
        }

        /// <summary>
        /// Returns an array of <see cref="DateKey"/>s based on the given from and to
        /// <see cref="ZonedDateTime"/> range.
        /// </summary>
        /// <param name="barType">The bar specification.</param>
        /// <param name="fromDate">The from date time.</param>
        /// <param name="toDate">The to date time.</param>
        /// <returns>An array of key <see cref="string"/>(s).</returns>
        /// <remarks>The given time range should have been previously validated.</remarks>
        internal static RedisKey[] GetBarsKeys(BarType barType, DateKey fromDate, DateKey toDate)
        {
            Debug.True(fromDate.CompareTo(toDate) <= 0, "fromDate.CompareTo(toDate) <= 0");

            return GetDateKeys(fromDate, toDate)
                .Select(key => GetBarsKey(barType, key))
                .ToArray();
        }

        /// <summary>
        /// Returns a wildcard key string for all instruments.
        /// </summary>
        /// <returns>The key <see cref="string"/>.</returns>
        internal static RedisValue GetInstrumentsPattern()
        {
            return InstrumentsNamespace + "*";
        }

        /// <summary>
        /// Returns the instruments key.
        /// </summary>
        /// <param name="symbol">The instruments symbol.</param>
        /// <returns>The key <see cref="string"/>.</returns>
        internal static RedisKey GetInstrumentsKey(Symbol symbol)
        {
            return InstrumentsNamespace + ":" + symbol.Value;
        }
    }
}
