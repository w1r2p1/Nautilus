﻿// -------------------------------------------------------------------------------------------------
// <copyright file="RedisBarClient.cs" company="Nautech Systems Pty Ltd.">
//   Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Redis
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.CQS;
    using Nautilus.Core.Validation;
    using Nautilus.Core.Extensions;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Database.Interfaces;
    using Nautilus.Database.Keys;
    using Nautilus.Database.Types;
    using Nautilus.Database.Wranglers;
    using NodaTime;
    using ServiceStack.Redis;

    /// <summary>
    /// A client for accessing bar data from <see cref="Redis"/> with a
    /// <see cref="RedisNativeClient"/>. This client is not thread-safe and therefor should be
    /// encapsulated in a thread-safe environment for sequential operations on the
    /// <see cref="Redis"/> database.
    /// </summary>
    [Immutable]
    public class RedisBarClient
    {
        private readonly RedisNativeClient redisClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisBarClient"/> class.
        /// </summary>
        /// <param name="redisEndpoint">The <see cref="Redis"/> end point.</param>
        /// <param name="compressor">The data compressor.</param>
        public RedisBarClient(RedisEndpoint redisEndpoint, IDataCompressor compressor)
        {
            Validate.NotNull(redisEndpoint, nameof(redisEndpoint));
            Validate.NotNull(compressor, nameof(compressor));

            this.redisClient = new RedisNativeClient(redisEndpoint);
        }

        /// <summary>
        /// Warning: Flushes ALL data from the <see cref="Redis"/> database.
        /// </summary>
        /// <param name="areYouSure">The are you sure string.
        /// </param>
        /// <returns>A <see cref="CommandResult"/> result.</returns>
        public CommandResult FlushAll(string areYouSure)
        {
            Debug.NotNull(areYouSure, nameof(areYouSure));

            if (areYouSure == "YES")
            {
                this.redisClient.FlushAll();

                return CommandResult.Ok();
            }

            return CommandResult.Fail("Database Flush not confirmed");
        }

        /// <summary>
        /// Returns a result indicating whether a <see cref="Redis"/> Key exists for the given
        /// <see cref="BarDataKey"/>.
        /// </summary>
        /// <param name="key">The <see cref="BarDataKey"/>.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool KeyExists(string key)
        {
            Debug.NotNull(key, nameof(key));

            return this.redisClient.Exists(key) == 1;
        }

        /// <summary>
        /// Returns a result indicating whether a <see cref="Redis"/> Key exists for the given
        /// <see cref="BarDataKey"/>.
        /// </summary>
        /// <param name="key">The <see cref="BarDataKey"/>.</param>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool KeyExists(BarDataKey key)
        {
            Debug.NotDefault(key, nameof(key));

            return this.KeyExists(key.ToString());
        }

        /// <summary>
        /// Returns a count of all bars held within the <see cref="Redis"/> namespace 'market_date'.
        /// </summary>
        /// <returns>A <see cref="long"/>.</returns>
        public long AllKeysCount()
        {
            return this.redisClient.Keys(KeyProvider.BarsNamespaceWildcard).Length;
        }

        /// <summary>
        /// Returns a count of all bars held within <see cref="Redis"/> of the given <see cref="BarSpecification"/>.
        /// </summary>
        /// <param name="barType">The bar specification.</param>
        /// <returns>A <see cref="long"/>.</returns>
        public long KeysCount(BarType barType)
        {
            Debug.NotNull(barType, nameof(barType));

            return this.redisClient.Keys(KeyProvider.GetBarsWildcardString(barType)).Length;
        }

        /// <summary>
        /// Returns a count of all bars held within <see cref="Redis"/> of the given <see cref="BarSpecification"/>.
        /// </summary>
        /// <param name="barType">The bar specification.</param>
        /// <returns>A <see cref="long"/>.</returns>
        public long BarsCount(BarType barType)
        {
            Debug.NotNull(barType, nameof(barType));

            var allKeys = this.redisClient.Keys(KeyProvider.GetBarsWildcardString(barType));

            if (allKeys.Length == 0)
            {
                return 0;
            }

            return allKeys
                .Select(key => Encoding.Default.GetString(key))
                .ToList()
                .Select(this.GetBarsByDay)
                .Sum(k => k.Value.Count);
        }

        /// <summary>
        /// Returns a count of all bar strings held within the <see cref="Redis"/> namespace 'MarketData'.
        /// </summary>
        /// <returns>A <see cref="long"/>.</returns>
        public long AllBarsCount()
        {
            var allbarTypeKeys = this.redisClient.Keys(KeyProvider.BarsNamespaceWildcard);

            if (allbarTypeKeys.Length == 0)
            {
                return 0;
            }

            return allbarTypeKeys
                .Select(key => Encoding.Default.GetString(key))
                .ToList()
                .Select(this.GetBarsByDay)
                .Sum(k => k.Value.Count);
        }

        /// <summary>
        /// Returns a list of all market data keys based on the given bar specification.
        /// </summary>
        /// <param name="barType">The bar specification.</param>
        /// <returns>A query result of <see cref="IReadOnlyList{T}"/> strings.</returns>
        public QueryResult<List<string>> GetAllSortedKeys(BarType barType)
        {
            Debug.NotNull(barType, nameof(barType));

            if (this.KeysCount(barType) == 0)
            {
                return QueryResult<List<string>>.Fail($"No market data found for {barType}");
            }

            var allKeysBytes = this.redisClient.Keys(KeyProvider.GetBarsWildcardString(barType));

            var keysCollection = allKeysBytes
                .Select(key => Encoding.Default.GetString(key))
                .ToList();

            keysCollection.Sort();

            return QueryResult<List<string>>.Ok(keysCollection);
        }

        /// <summary>
        /// Adds the given bar to the <see cref="Redis"/> List associated with the
        /// <see cref="BarDataKey"/>.
        /// </summary>
        /// <param name="barType">The bar type to add.</param>
        /// <param name="bar">The bar to add.</param>
        /// <returns>A command result.</returns>
        [PerformanceOptimized]
        public CommandResult AddBar(BarType barType, Bar bar)
        {
            Debug.NotNull(barType, nameof(barType));

            var dateKey = new DateKey(bar.Timestamp);
            var key = new BarDataKey(barType, dateKey);
            var keyString = key.ToString();

            this.redisClient.RPush(keyString, bar.ToUtf8Bytes());

            return CommandResult.Ok(
                $"Added 1 bar to {barType}");
        }

        /// <summary>
        /// Adds the given bars to the <see cref="Redis"/> Lists associated with their
        /// <see cref="BarDataKey"/>(s).
        /// </summary>
        /// <param name="barType">The bar type.</param>
        /// <param name="bars">The bars to add.</param>
        /// <returns>A command result.</returns>
        [PerformanceOptimized]
        public CommandResult AddBars(BarType barType, Bar[] bars)
        {
            Debug.NotNull(barType, nameof(barType));
            Debug.EqualTo(1, nameof(barType.Specification.Period), barType.Specification.Period);
            Debug.CollectionNotNullOrEmpty(bars, nameof(bars));

            var barsIndex = BarWrangler.OrganizeBarsByDay(bars);
            var barsAddedCounter = 0;

            foreach (var barsToAddDictionary in barsIndex)
            {
                var key = new BarDataKey(barType, barsToAddDictionary.Key);
                var keyString = key.ToString();

                if (!this.KeyExists(key))
                {
                    foreach (var bar in barsToAddDictionary.Value)
                    {
                        this.redisClient.RPush(keyString, bar.ToUtf8Bytes());
                        barsAddedCounter++;
                    }

                    continue;
                }

                // The key should exist in Redis because it was just checked by KeyExists().
                var persistedBars = this.GetBarsByDay(keyString).Value;

                foreach (var bar in barsToAddDictionary.Value)
                {
                    if (bar.Timestamp.IsGreaterThan(persistedBars.Last().Timestamp))
                    {
                        this.redisClient.RPush(keyString, bar.ToUtf8Bytes());
                        barsAddedCounter++;
                    }
                }
            }

            return CommandResult.Ok(
                $"Added {barsAddedCounter} bars to {barType} (TotalCount={this.BarsCount(barType)})");
        }

        /// <summary>
        /// Returns all bars from <see cref="Redis"/> of the given <see cref="BarSpecification"/>.
        /// </summary>
        /// <param name="barType">The specification of bars to get.</param>
        /// <returns>A read only collection of <see cref="Bar"/>(s).</returns>
        [PerformanceOptimized]
        public QueryResult<BarDataFrame> GetAllBars(BarType barType)
        {
            Debug.NotNull(barType, nameof(barType));
            Debug.EqualTo(1, nameof(barType.Specification.Period), barType.Specification.Period);

            var barKeysQuery = this.GetAllSortedKeys(barType);

            if (barKeysQuery.IsFailure)
            {
                return QueryResult<BarDataFrame>.Fail(barKeysQuery.Message);
            }

            var barsArray = barKeysQuery
                .Value
                .SelectMany(key => BarWrangler.ParseBars(this.redisClient.LRange(key, 0, -1)))
                .ToArray();

            return QueryResult<BarDataFrame>.Ok(new BarDataFrame(barType, barsArray));
        }

        /// <summary>
        /// Returns all bars from <see cref="Redis"/> of the given <see cref="BarSpecification"/> within the given
        /// range of <see cref="ZonedDateTime"/> (inclusive).
        /// </summary>
        /// <param name="barType">The specification of bars to get.</param>
        /// <param name="fromDateTime">The from date time range.</param>
        /// <param name="toDateTime">The to date time range.</param>
        /// <returns>A read only collection of <see cref="Bar"/>(s).</returns>
        public QueryResult<BarDataFrame> GetBars(
            BarType barType,
            ZonedDateTime fromDateTime,
            ZonedDateTime toDateTime)
        {
            Debug.NotNull(barType, nameof(barType));
            Debug.NotDefault(fromDateTime, nameof(fromDateTime));
            Debug.NotDefault(toDateTime, nameof(toDateTime));

            if (this.KeysCount(barType) == 0)
            {
                return QueryResult<BarDataFrame>.Fail($"No market data found for {barType}");
            }

            var barKeysQuery = KeyProvider.GetBarsKeyStrings(barType, fromDateTime, toDateTime);

            var barsArray = barKeysQuery
                .Select(key => this.redisClient.LRange(key, 0, -1))
                .SelectMany(BarWrangler.ParseBars)
                .Where(bar => bar.Timestamp.Compare(fromDateTime) >= 0 && bar.Timestamp.Compare(toDateTime) <= 0)
                .ToArray();

            if (barsArray.Length == 0)
            {
                return QueryResult<BarDataFrame>.Fail(
                    $"No market data found for {barType} in time range from " +
                    $"{fromDateTime.ToIsoString()} to " +
                    $"{toDateTime.ToIsoString()}");
            }

            return QueryResult<BarDataFrame>.Ok(new BarDataFrame(barType, barsArray));
        }

        /// <summary>
        /// Returns a query result if success containing the requested bar, or failure containing
        /// a message.
        /// </summary>
        /// <param name="barType">The requested bars specification.</param>
        /// <param name="timestamp">The requested bars timestamp.</param>
        /// <returns>A query result of <see cref="Bar"/>.</returns>
        [PerformanceOptimized]
        public QueryResult<Bar> GetBar(BarType barType, ZonedDateTime timestamp)
        {
            Debug.NotNull(barType, nameof(barType));
            Debug.NotDefault(timestamp, nameof(timestamp));

            var key = new BarDataKey(barType, new DateKey(timestamp));
            var persistedBarsQuery = this.GetBarsByDay(key.ToString());

            if (persistedBarsQuery.IsFailure)
            {
                return QueryResult<Bar>.Fail(persistedBarsQuery.Message);
            }

            for (var i = 0; i < persistedBarsQuery.Value.Count; i++)
            {
                if (persistedBarsQuery.Value[i].Timestamp.Equals(timestamp))
                {
                    return QueryResult<Bar>.Ok(persistedBarsQuery.Value[i]);
                }
            }

            return QueryResult<Bar>.Fail(
                $"No market data found for {barType} at {timestamp.ToIsoString()}");
        }

        /// <summary>
        /// Finds and returns bars by the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The query result list of bars.</returns>
        public QueryResult<List<Bar>> GetBarsByDay(string key)
        {
            Debug.NotNull(key, nameof(key));

            if (!this.KeyExists(key))
            {
                return QueryResult<List<Bar>>.Fail(
                    $"No market data found for {key}");
            }

            var barBytes = this.redisClient.LRange(key, 0, -1);

            return QueryResult<List<Bar>>.Ok(BarWrangler.ParseBars(barBytes));
        }
    }
}