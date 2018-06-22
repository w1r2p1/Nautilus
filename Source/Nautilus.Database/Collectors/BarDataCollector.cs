﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MarketDataCollector.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Database.Collectors
{
    using System;
    using Nautilus.Common.Componentry;
    using Nautilus.Database.Interfaces;
    using Nautilus.Database.Messages.Commands;
    using Nautilus.Database.Orchestration;
    using Nautilus.Common.Enums;
    using Nautilus.Core;
    using Nautilus.Core.Extensions;
    using Nautilus.Core.Validation;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages;
    using Nautilus.Database.Messages.Documents;
    using Nautilus.Database.Types;
    using Nautilus.DomainModel.Factories;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;

    /// <summary>
    /// Represents a market data collector.
    /// </summary>
    public class BarDataCollector : ActorComponentBase
    {
        private readonly IBarDataReader dataReader;
        private readonly DataCollectionSchedule collectionSchedule;
        private Option<ZonedDateTime?> lastPersistedBarTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarDataCollector"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="dataReader">The bar data reader.</param>
        /// <param name="collectionSchedule">The collection schedule.</param>
        public BarDataCollector(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            IBarDataReader dataReader,
            DataCollectionSchedule collectionSchedule)
            : base(
                ServiceContext.Database,
                LabelFactory.Component($"{nameof(BarDataCollector)}-{dataReader.BarType}"),
                container)
        {
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(dataReader, nameof(dataReader));
            Validate.NotNull(collectionSchedule, nameof(collectionSchedule));

            this.dataReader = dataReader;
            this.collectionSchedule = collectionSchedule;

            this.Receive<StartSystem>(msg => this.OnMessage(msg));
            this.Receive<CollectData<BarType>>(msg => this.OnMessage(msg));
            this.Receive<DataStatusResponse<ZonedDateTime>>(msg => this.OnMessage(msg));
            this.Receive<DataPersisted<BarType>>(msg => this.OnMessage(msg));
        }

        private void OnMessage(StartSystem message)
        {
            Debug.NotNull(message, nameof(message));
        }

        private void OnMessage(CollectData<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            if (this.dataReader.GetAllCsvFilesOrdered().IsFailure)
            {
                this.Log.Warning($"No csv files found for {this.dataReader.BarType}");

                Context.Parent.Tell(new DataCollected<BarType>(this.dataReader.BarType, Guid.NewGuid(), this.TimeNow()), this.Self);

                return;
            }

            foreach (var csv in this.dataReader.GetAllCsvFilesOrdered().Value)
            {
                // TODO: Changed to just read all bars to get all data in.
                var csvQuery = this.dataReader.GetAllBars(csv);

                if (csvQuery.IsSuccess)
                {
//                    // TODO: Temporary work around of bottleneck
//                    this.Logger.Information($"{this.ComponentName} delaying 30s to allow repository to persist...");
//                    Thread.Sleep(TimeSpan.FromSeconds(30));

                    Context.Parent.Tell(
                        new DataDelivery<BarDataFrame>(
                            csvQuery.Value,
                            Guid.NewGuid(),
                            this.TimeNow()),
                        this.Self);

                    this.collectionSchedule.UpdateLastCollectedTime(this.TimeNow());

                    //this.Log(LogLevel.Debug, $"{this.Component} collected {csvQuery.Value.Bars.Length} {csvQuery.Value.barTypeification} bars");
                    this.Log.Debug($"Updated last collected time to {this.collectionSchedule.LastCollectedTime.Value.ToIsoString()}");
                }

                if (csvQuery.IsFailure)
                {
                    this.Log.Warning(csvQuery.Message);
                }
            }

            Context.Parent.Tell(new DataCollected<BarType>(this.dataReader.BarType, Guid.NewGuid(), this.TimeNow()), this.Self);
        }

        private void OnMessage(DataStatusResponse<ZonedDateTime> message)
        {
            Debug.NotNull(message, nameof(message));

            if (message.LastTimestampQuery.IsSuccess)
            {
                this.lastPersistedBarTime = message.LastTimestampQuery.Value;

                this.Log.Debug(
                    $"From {nameof(DataStatusResponse<ZonedDateTime>)} " +
                    $"updated last persisted bar timestamp to {this.lastPersistedBarTime.Value.ToIsoString()}");

                return;
            }

            this.Log.Debug(
                $"From {nameof(DataStatusResponse<ZonedDateTime>)} " +
                $"no persisted bar timestamp");
        }

        private void OnMessage(DataPersisted<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            this.lastPersistedBarTime = message.LastDataTime;
        }
    }
}