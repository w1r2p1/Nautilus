﻿//--------------------------------------------------------------------------------------------------
// <copyright file="DataService.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Data
{
    using System;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Common.Messages.Events;
    using Nautilus.Common.Messages.Jobs;
    using Nautilus.Common.Messaging;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.DomainModel.Factories;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Scheduler.Commands;
    using Nautilus.Scheduler.Events;
    using Quartz;

    /// <summary>
    /// The main macro object which contains the <see cref="DataService"/> and presents its API.
    /// </summary>
    [PerformanceOptimized]
    public sealed class DataService : ActorComponentBusConnectedBase
    {
        private readonly IFixGateway gateway;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataService"/> class.
        /// </summary>
        /// <param name="setupContainer">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="gateway">The FIX gateway.</param>
        /// <exception cref="ValidationException">Throws if the validation fails.</exception>
        public DataService(
            IComponentryContainer setupContainer,
            IMessagingAdapter messagingAdapter,
            IFixGateway gateway)
            : base(
                NautilusService.Data,
                LabelFactory.Component(nameof(DataService)),
                setupContainer,
                messagingAdapter)
        {
            Validate.NotNull(setupContainer, nameof(setupContainer));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));
            Validate.NotNull(gateway, nameof(gateway));

            this.gateway = gateway;

            // Command messages.
            this.Receive<ConnectFixJob>(this.OnMessage);
            this.Receive<DisconnectFixJob>(this.OnMessage);
            this.Receive<Subscribe<BarType>>(this.OnMessage);
            this.Receive<Unsubscribe<BarType>>(this.OnMessage);

            // Event messages.
            this.Receive<JobCreated>(this.OnMessage);
            this.Receive<FixSessionConnected>(this.OnMessage);
            this.Receive<FixSessionDisconnected>(this.OnMessage);
        }

        /// <summary>
        /// Start method called when the <see cref="StartSystem"/> message is received.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void Start(StartSystem message)
        {
            this.Log.Information($"Started at {this.StartTime}.");

            this.CreateConnectFixJob();
            this.CreateDisconnectFixJob();
        }

        private void CreateConnectFixJob()
        {
            var scheduleBuilder = CronScheduleBuilder
                .WeeklyOnDayAndHourAndMinute(DayOfWeek.Sunday, 20, 00)
                .InTimeZone(TimeZoneInfo.Utc)
                .WithMisfireHandlingInstructionFireAndProceed();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity($"connect_fix", "fix44")
                .WithSchedule(scheduleBuilder)
                .Build();

            var createJob = new CreateJob(
                new ActorEndpoint(this.Self),
                new ActorEndpoint(this.Self),
                new ConnectFixJob(),
                trigger,
                this.NewGuid(),
                this.TimeNow());

            this.Send(ServiceAddress.Scheduler, createJob);
            this.Log.Information("Created ConnectFixJob for Sundays 20:00 (UTC).");
        }

        private void CreateDisconnectFixJob()
        {
            var scheduleBuilder = CronScheduleBuilder
                .WeeklyOnDayAndHourAndMinute(DayOfWeek.Saturday, 20, 00)
                .InTimeZone(TimeZoneInfo.Utc)
                .WithMisfireHandlingInstructionFireAndProceed();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity($"disconnect_fix", "fix44")
                .WithSchedule(scheduleBuilder)
                .Build();

            var createJob = new CreateJob(
                new ActorEndpoint(this.Self),
                new ActorEndpoint(this.Self),
                new DisconnectFixJob(),
                trigger,
                this.NewGuid(),
                this.TimeNow());

            this.Send(ServiceAddress.Scheduler, createJob);
            this.Log.Information("Created DisconnectFixJob for Saturdays 20:00 (UTC).");
        }

        private void OnMessage(ConnectFixJob message)
        {
            this.gateway.Connect();
        }

        private void OnMessage(DisconnectFixJob message)
        {
            this.gateway.Disconnect();
        }

        private void OnMessage(Subscribe<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            this.Send(DataServiceAddress.DataCollectionManager, message);
        }

        private void OnMessage(Unsubscribe<BarType> message)
        {
            Debug.NotNull(message, nameof(message));

            this.Send(DataServiceAddress.DataCollectionManager, message);
        }

        private void OnMessage(FixSessionConnected message)
        {
            Debug.NotNull(message, nameof(message));

            this.Log.Information($"{message.SessionId} session is connected.");

            this.gateway.UpdateInstrumentsSubscribeAll();
            this.gateway.MarketDataSubscribeAll();
        }

        private void OnMessage(FixSessionDisconnected message)
        {
            Debug.NotNull(message, nameof(message));

            this.Log.Warning($"{message.SessionId} session has been disconnected.");
        }

        private void OnMessage(JobCreated message)
        {
            // Do nothing.
        }
    }
}
