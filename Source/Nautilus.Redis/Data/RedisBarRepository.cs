﻿// -------------------------------------------------------------------------------------------------
// <copyright file="RedisBarRepository.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Redis.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using Nautilus.Core.CQS;
    using Nautilus.Data.Interfaces;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Frames;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;
    using StackExchange.Redis;

    /// <summary>
    /// Provides a repository for persisting <see cref="Bar"/> objects into Redis.
    /// </summary>
    public sealed class RedisBarRepository : IBarRepository
    {
        private readonly RedisBarClient barClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisBarRepository"/> class.
        /// </summary>
        /// <param name="connection">The clients manager.</param>
        public RedisBarRepository(ConnectionMultiplexer connection)
        {
            this.barClient = new RedisBarClient(connection);
        }

        /// <summary>
        /// Returns the total count of bars persisted within the database.
        /// </summary>
        /// <returns>A <see cref="int"/>.</returns>
        public int AllBarsCount()
        {
            return this.barClient.AllBarsCount();
        }

        /// <summary>
        /// Returns the count of bars persisted within the database with the given
        /// <see cref="BarSpecification"/>.
        /// </summary>
        /// <param name="barType">The bar specification.</param>
        /// <returns>A <see cref="int"/>.</returns>
        public int BarsCount(BarType barType)
        {
            return this.barClient.BarsCount(barType);
        }

        /// <summary>
        /// Adds the given bar to the repository.
        /// </summary>
        /// <param name="barType">The bar type to add.</param>
        /// <param name="bar">The bar to add.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public CommandResult Add(BarType barType, Bar bar)
        {
            return this.barClient.AddBar(barType, bar);
        }

        /// <summary>
        /// Adds the given bar(s) to the repository.
        /// </summary>
        /// <param name="barData">The data data to add.</param>
        /// <returns>A <see cref="CommandResult"/>.</returns>
        public CommandResult Add(BarDataFrame barData)
        {
            return this.barClient.AddBars(barData.BarType, barData.Bars);
        }

        /// <summary>
        /// Returns a market data frame populated with the given bar info specification.
        /// </summary>
        /// <param name="barType">The bar type.</param>
        /// <param name="fromDateTime">The from date time.</param>
        /// <param name="toDateTime">The to date time.</param>
        /// <returns>A <see cref="QueryResult{MarketDataFrame}"/>.</returns>
        public QueryResult<BarDataFrame> Find(
            BarType barType,
            ZonedDateTime fromDateTime,
            ZonedDateTime toDateTime)
        {
            var barsQuery = this.barClient.GetBars(barType, fromDateTime, toDateTime);

            return barsQuery.IsSuccess
                 ? QueryResult<BarDataFrame>.Ok(barsQuery.Value)
                 : QueryResult<BarDataFrame>.Fail(barsQuery.Message);
        }

        /// <summary>
        /// Finds and returns all bars matching the given bar type.
        /// </summary>
        /// <param name="barType">The bar type.</param>
        /// <returns>The query result of bars.</returns>
        public QueryResult<BarDataFrame> FindAll(BarType barType)
        {
            var barsQuery = this.barClient.GetAllBars(barType);

            return barsQuery.IsSuccess
                ? QueryResult<BarDataFrame>.Ok(barsQuery.Value)
                : QueryResult<BarDataFrame>.Fail(barsQuery.Message);
        }

        /// <summary>
        /// Returns a query result containing the <see cref="ZonedDateTime"/> timestamp of the last
        /// bar within <see cref="Redis"/> for the given <see cref="BarSpecification"/> (if successful).
        /// </summary>
        /// <param name="barType">The requested bar type.</param>
        /// <returns>A <see cref="QueryResult{T}"/> containing the <see cref="Bar"/>.</returns>
        public QueryResult<ZonedDateTime> LastBarTimestamp(BarType barType)
        {
            var barKeysQuery = this.barClient.GetAllSortedKeys(barType);

            if (barKeysQuery.IsFailure)
            {
                return QueryResult<ZonedDateTime>.Fail(barKeysQuery.Message);
            }

            var lastKey = barKeysQuery.Value.Last();

            var barsQuery = this.barClient.GetBarsByDay(lastKey);

            return barsQuery.IsSuccess
                ? QueryResult<ZonedDateTime>.Ok(barsQuery.Value.Last().Timestamp)
                : QueryResult<ZonedDateTime>.Fail(barsQuery.Message);
        }

        /// <summary>
        /// Removes the difference in date keys for each symbol from the database.
        /// </summary>
        /// <param name="resolution">The resolution to trim.</param>
        /// <param name="trimToDays">The number of days (keys) to trim to.</param>
        /// <returns>The result of the operation.</returns>
        public CommandResult TrimToDays(Resolution resolution, int trimToDays)
        {
            var results = new List<CommandResult>();
            var keys = this.barClient.GetSortedKeysBySymbolResolution(resolution);

            foreach (var value in keys.Values)
            {
                var keyCount = value.Count;
                if (keyCount <= trimToDays)
                {
                    continue;
                }

                var difference = keyCount - trimToDays;
                for (var i = 0; i < difference; i++)
                {
                    var result = this.barClient.Delete(value[i]);
                    results.Add(result);
                }
            }

            return CommandResult.Combine(results.ToArray());
        }

        /// <summary>
        /// Sends a command to Redis to snapshot the database to disk.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public CommandResult SnapshotDatabase()
        {
            // Not implemented.
            return CommandResult.Ok();
        }
    }
}