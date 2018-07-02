﻿//--------------------------------------------------------------------------------------------------
// <copyright file="DataCollectionManager.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Database
{
    using System;
    using System.Collections.Generic;
    using Akka.Actor;
    using Nautilus.Core.Validation;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages;
    using Nautilus.Core.Collections;
    using Nautilus.Database.Enums;
    using Nautilus.Database.Jobs;
    using Nautilus.Database.Messages.Commands;
    using Nautilus.Database.Messages.Documents;
    using Nautilus.Database.Messages.Events;
    using Nautilus.Database.Types;
    using Nautilus.DomainModel.Enums;
    using Nautilus.DomainModel.Factories;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Scheduler.Commands;
    using Quartz;

    /// <summary>
    /// The manager class which orchestrates data collection operations.
    /// </summary>
    public class DataCollectionManager : ActorComponentBusConnectedBase
    {
        private readonly IComponentryContainer storedContainer;
        private readonly IActorRef barPublisher;
        private readonly ReadOnlyList<Resolution> resolutionsToPersist;
        private readonly int barRollingWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionManager"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="barPublisher">The bar publisher.</param>
        /// <param name="resolutionsToPersist">The bar resolutions to persist (with a period of 1).</param>
        /// <param name="barRollingWindow">The rolling window of persisted bar data (days).</param>
        /// <exception cref="ValidationException">Throws if the validation fails.</exception>
        public DataCollectionManager(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            IActorRef barPublisher,
            IReadOnlyList<Resolution> resolutionsToPersist,
            int barRollingWindow)
            : base(
                ServiceContext.Database,
                LabelFactory.Component(nameof(DataCollectionManager)),
                container,
                messagingAdapter)
        {
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));
            Validate.NotNull(barPublisher, nameof(barPublisher));
            Validate.Int32NotOutOfRange(barRollingWindow, nameof(barRollingWindow), 0, int.MaxValue, RangeEndPoints.LowerExclusive);

            this.storedContainer = container;
            this.barPublisher = barPublisher;
            this.resolutionsToPersist = new ReadOnlyList<Resolution>(resolutionsToPersist);
            this.barRollingWindow = barRollingWindow;

            // Command messages
            this.Receive<StartSystem>(msg => this.OnMessage(msg));
            this.Receive<Subscribe<BarType>>(msg => this.OnMessage(msg));
            this.Receive<CollectData<BarType>>(msg => this.OnMessage(msg));
            this.Receive<TrimBarDataJob>(msg => this.OnMessage(msg));

            // Document messages
            this.Receive<DataDelivery<BarClosed>>(msg => this.OnMessage(msg));
            this.Receive<DataDelivery<BarDataFrame>>(msg => this.OnMessage(msg));
            this.Receive<DataPersisted<BarType>>(msg => this.OnMessage(msg));
            this.Receive<DataCollected<BarType>>(msg => this.OnMessage(msg));
        }

        private void OnMessage(StartSystem message)
        {
            Debug.NotNull(message, nameof(message));

            // Do nothing.
        }

        private void OnMessage(Subscribe<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            this.Send(DatabaseService.BarAggregationController, message);
        }

        private void OnMessage(CollectData<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            // Do nothing.
        }

        private void OnMessage(DataDelivery<BarClosed> message)
        {
            Debug.NotNull(message, nameof(message));

            this.barPublisher.Tell(message.Data);
            this.Send(DatabaseService.TaskManager, message);
        }

        private void OnMessage(DataDelivery<BarDataFrame> message)
        {
            Debug.NotNull(message, nameof(message));

//            if (this.barDataProvider.IsBarDataCheckOn)
//            {
//                var result = BarDataChecker.CheckBars(
//                    message.Data.BarType,
//                    message.Data.Bars);
//
//                if (result.IsSuccess)
//                {
//                    if (result.Value.Count == 0)
//                    {
//                        this.Log.Information(result.Message);
//                    }
//
//                    if (result.Value.Count > 0)
//                    {
//                        this.Log.Warning(result.Message);
//
//                        foreach (var anomaly in result.Value)
//                        {
//                            this.Log.Warning(anomaly);
//                        }
//                    }
//                }
//
//                if (result.IsFailure)
//                {
//                    this.Log.Warning(result.FullMessage);
//                }
//            }
//
//            this.Send(DatabaseService.TaskManager, message);
        }

        private void OnMessage(DataPersisted<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            // Do nothing.
        }

        private void OnMessage(DataCollected<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            // Not implemented.
        }

        private void OnMessage(TrimBarDataJob message)
        {
            Debug.NotNull(message, nameof(message));

            var command = new TrimBarData(
                this.resolutionsToPersist,
                this.barRollingWindow,
                this.NewGuid(),
                this.TimeNow());

            this.Send(DatabaseService.TaskManager, command);

            this.Log.Debug($"Received {nameof(TrimBarDataJob)}.");
        }

        private void CreateTrimBarDataJob()
        {
            var scheduleBuilder = CronScheduleBuilder
                .WeeklyOnDayAndHourAndMinute(DayOfWeek.Sunday, 00, 01)
                .InTimeZone(TimeZoneInfo.Utc)
                .WithMisfireHandlingInstructionFireAndProceed();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity($"trim_bar_data", "data_management")
                .WithSchedule(scheduleBuilder)
                .Build();

            var createJob = new CreateJob(
                this.Self,
                new TrimBarDataJob(),
                trigger,
                this.NewGuid(),
                this.TimeNow());

            this.Send(DatabaseService.Scheduler, createJob);

            this.Log.Debug($"Created {nameof(TrimBarDataJob)}, sent to scheduler.");
        }

        private void InitializeMarketDataCollectors()
        {
            // Not implemented.
        }

        private void CollectMarketData()
        {
            // Not implemented.
        }
    }
}
