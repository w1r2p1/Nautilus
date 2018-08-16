// -------------------------------------------------------------------------------------------------
// <copyright file="RabbitMQServer.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.RabbitMQ
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;
    using Nautilus.Common.Commands;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.DomainModel.Aggregates;
    using Nautilus.DomainModel.Events;
    using Nautilus.DomainModel.Factories;

    /// <summary>
    /// Provides a RabbitMQ message broker implementation.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Reviewed. Suppression OK.")]
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "Reviewed. Suppression OK.")]
    [PerformanceOptimized]
    public class RabbitMQServer : ActorComponentBusConnectedBase
    {
        private readonly ICommandSerializer commandSerializer;
        private readonly IEventSerializer eventSerializer;
        private readonly IConnection commandConnection;
        private readonly IConnection eventConnection;
        private readonly IModel commandChannel;
        private readonly IModel eventChannel;
        private readonly List<Order> orders;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQServer"/> class.
        /// </summary>
        /// <param name="container">The setup container.</param>
        /// <param name="messagingAdapter">The messaging adapter.</param>
        /// <param name="commandSerializer">The command serializer.</param>
        /// <param name="eventSerializer">The event serializer.</param>
        /// <param name="commandConnection">The command connection.</param>
        /// <param name="eventConnection">The event connection.</param>
        public RabbitMQServer(
            IComponentryContainer container,
            IMessagingAdapter messagingAdapter,
            ICommandSerializer commandSerializer,
            IEventSerializer eventSerializer,
            IConnection commandConnection,
            IConnection eventConnection)
            : base(
                NautilusService.Messaging,
                LabelFactory.Component(nameof(RabbitMQServer)),
                container,
                messagingAdapter)
        {
            Validate.NotNull(container, nameof(container));
            Validate.NotNull(messagingAdapter, nameof(messagingAdapter));
            Validate.NotNull(commandSerializer, nameof(commandSerializer));
            Validate.NotNull(eventSerializer, nameof(eventSerializer));
            Validate.NotNull(commandConnection, nameof(commandConnection));
            Validate.NotNull(eventConnection, nameof(eventConnection));

            this.commandSerializer = commandSerializer;
            this.eventSerializer = eventSerializer;
            this.commandConnection = commandConnection;
            this.eventConnection = eventConnection;
            this.orders = new List<Order>();

            try
            {
                this.commandChannel = this.commandConnection.CreateModel();
                this.commandChannel.ExchangeDeclare(
                    RabbitConstants.ExecutionCommandsExchange,
                    ExchangeType.Direct,
                    durable: true);
                this.Log.Information($"Exchange {RabbitConstants.ExecutionCommandsExchange} declared.");

                this.commandChannel.QueueDeclare(
                    RabbitConstants.InvTraderCommandsQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                this.Log.Information($"Queue {RabbitConstants.InvTraderCommandsQueue} declared.");

                this.commandChannel.QueueBind(
                    RabbitConstants.InvTraderCommandsQueue,
                    RabbitConstants.ExecutionCommandsExchange,
                    RabbitConstants.InvTraderCommandsQueue);
                this.Log.Information($"Queue {RabbitConstants.InvTraderCommandsQueue} bound to {RabbitConstants.ExecutionCommandsExchange}.");

                var consumer = new EventingBasicConsumer(this.commandChannel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var command = this.commandSerializer.Deserialize(body);

                    if (command is SubmitOrder submitOrder)
                    {
                        var orderToSubmit = submitOrder.Order;
                        var order = new Order(
                            orderToSubmit.Symbol,
                            orderToSubmit.Id,
                            orderToSubmit.Label,
                            orderToSubmit.Side,
                            orderToSubmit.Type,
                            orderToSubmit.Quantity,
                            orderToSubmit.Price,
                            orderToSubmit.TimeInForce,
                            orderToSubmit.ExpireTime,
                            orderToSubmit.Timestamp);

                        this.orders.Add(order);
                    }
                    else if (command is ModifyOrder modifyOrder)
                    {
                        var order = this.orders.FirstOrDefault(o => o.Id == modifyOrder.Order.Id);

                        if (order is null)
                        {
                            this.Log.Warning("Order not found for ModifyOrder command.");
                            return;
                        }

                        command = new ModifyOrder(
                            order,
                            modifyOrder.ModifiedPrice,
                            modifyOrder.Id,
                            modifyOrder.Timestamp);
                    }

                    this.Log.Debug($"Received {command}.");
                    this.Send(NautilusService.Execution, command);
                };
                this.Log.Information($"Basic event consumer created.");

                this.commandChannel.BasicConsume(
                    queue: RabbitConstants.InvTraderCommandsQueue,
                    autoAck: true,
                    consumer: consumer);

                this.eventChannel = this.eventConnection.CreateModel();
                this.eventChannel.ExchangeDeclare(
                    RabbitConstants.ExecutionEventsExchange,
                    ExchangeType.Fanout,
                    durable: true);
                this.Log.Information($"Exchange {RabbitConstants.ExecutionEventsExchange} declared.");

                this.eventChannel.QueueDeclare(
                    RabbitConstants.InvTraderEventsQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
                this.Log.Information($"Queue {RabbitConstants.InvTraderEventsQueue} declared.");

                this.eventChannel.QueueBind(
                    RabbitConstants.InvTraderEventsQueue,
                    RabbitConstants.ExecutionEventsExchange,
                    RabbitConstants.InvTraderEventsQueue);
                this.Log.Information($"Queue {RabbitConstants.InvTraderEventsQueue} bound to {RabbitConstants.ExecutionEventsExchange}.");
            }
            catch (Exception ex)
            {
                this.Log.Error($"Error {ex.Message}", ex);
                throw;
            }

            // Event messages
            this.Receive<Event>(this.OnMessage);
        }

        [PerformanceOptimized]
        private void OnMessage(Event @event)
        {
            Debug.NotNull(@event, nameof(@event));

            this.Log.Debug($"Event {@event} received.");

            if (@event is OrderEvent orderEvent)
            {
                var order = this.orders.FirstOrDefault(o => o.Id == orderEvent.OrderId);

                if (order is null)
                {
                    this.Log.Warning("Order not found for ModifyOrder command.");
                    return;
                }

                order.Apply(orderEvent);
            }

            this.eventChannel.BasicPublish(
                RabbitConstants.ExecutionEventsExchange,
                RabbitConstants.InvTraderEventsQueue,
                mandatory: false,
                basicProperties: null,
                body: this.eventSerializer.Serialize(@event));

            this.Log.Debug($"Published event {@event}.");
        }
    }
}
