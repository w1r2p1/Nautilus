﻿// -------------------------------------------------------------------------------------------------
// <copyright file="KeyProvider.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Redis
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
        public static List<DateKey> GetDateKeys(ZonedDateTime fromDateTime, ZonedDateTime toDateTime)
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
        public static List<DateKey> GetDateKeys(DateKey fromDate, DateKey toDate)
        {
            Debug.True(fromDate.CompareTo(toDate) <= 0, "fromDate.CompareTo(toDate) <= 0");

            var difference = (int)((toDate.StartOfDay - fromDate.StartOfDay) / Duration.FromDays(1));
            var dateRange = new List<DateKey> { fromDate };
            for (var i = 0; i < difference; i++)
            {
                dateRange.Add(new DateKey(fromDate.DateUtc + Period.FromDays(i + 1)));
            }

            return dateRange;
        }

        /// <summary>
        /// Returns a wildcard string from the given symbol bar spec.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public static string GetTickWildcardKey(Symbol symbol)
        {
            return $"{TicksNamespace}:{symbol.Venue.Value}:{symbol.Code}";
        }

        /// <summary>
        /// Returns the tick data key for the given parameters.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="dateKey">The date key.</param>
        /// <returns>The key string.</returns>
        public static string GetTickKey(Symbol symbol, DateKey dateKey)
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
        public static string[] GetTickKeys(Symbol symbol, DateKey fromDate, DateKey toDate)
        {
            return GetDateKeys(fromDate, toDate)
                .Select(key => GetTickKey(symbol, key))
                .ToArray();
        }

        /// <summary>
        /// Returns a wildcard key string for all bars.
        /// </summary>
        /// <returns>The key <see cref="string"/>.</returns>
        public static string GetBarWildcardKey()
        {
            return BarsNamespace + "*";
        }

        /// <summary>
        /// Returns a wildcard string from the given symbol bar spec.
        /// </summary>
        /// <param name="barType">The symbol bar spec.</param>
        /// <returns>A <see cref="string"/>.</returns>
        public static string GetBarWildcardKey(BarType barType)
        {
            return BarsNamespace +
                   $":{barType.Symbol.Venue.Value}" +
                   $":{barType.Symbol.Code}" +
                   $":{barType.Specification.BarStructure}" +
                   $":{barType.Specification.PriceType}" + "*";
        }

        /// <summary>
        /// Returns the bar data key for the given parameters.
        /// </summary>
        /// <param name="barType">The bar type.</param>
        /// <param name="dateKey">The date key.</param>
        /// <returns>The key <see cref="string"/>.</returns>
        public static string GetBarKey(BarType barType, DateKey dateKey)
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
        public static string[] GetBarKeys(BarType barType, DateKey fromDate, DateKey toDate)
        {
            Debug.True(fromDate.CompareTo(toDate) <= 0, "fromDate.CompareTo(toDate) <= 0");

            return GetDateKeys(fromDate, toDate)
                .Select(key => GetBarKey(barType, key))
                .ToArray();
        }

        /// <summary>
        /// Returns a wildcard key string for all instruments.
        /// </summary>
        /// <returns>The key <see cref="string"/>.</returns>
        public static string GetInstrumentWildcardKey()
        {
            return InstrumentsNamespace + "*";
        }

        /// <summary>
        /// Returns the instruments key.
        /// </summary>
        /// <param name="symbol">The instruments symbol.</param>
        /// <returns>The key <see cref="string"/>.</returns>
        public static string GetInstrumentKey(Symbol symbol)
        {
            return InstrumentsNamespace + ":" + symbol.Value;
        }
    }
}
