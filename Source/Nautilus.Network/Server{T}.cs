// -------------------------------------------------------------------------------------------------
// <copyright file="Server{T}.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   http://www.nautechsystems.net
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Network
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Nautilus.Common.Componentry;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Common.Messages.Responses;
    using Nautilus.Core;
    using Nautilus.Core.Correctness;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// The base class for all messaging servers.
    /// </summary>
    /// <typeparam name="T">The inbound message type.</typeparam>
    public abstract class Server<T> : Component
        where T : Message
    {
        private readonly IMessageSerializer<T> inboundSerializer;
        private readonly IMessageSerializer<Response> outboundSerializer;
        private readonly CancellationTokenSource cts;
        private readonly RouterSocket socket;

        private bool isReceiving;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server{TInbound}"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="inboundSerializer">The inbound message serializer.</param>
        /// <param name="outboundSerializer">The outbound message serializer.</param>
        /// <param name="host">The consumer host address.</param>
        /// <param name="port">The consumer port.</param>
        /// <param name="id">The consumer identifier.</param>
        protected Server(
            IComponentryContainer container,
            IMessageSerializer<T> inboundSerializer,
            IMessageSerializer<Response> outboundSerializer,
            NetworkAddress host,
            NetworkPort port,
            Guid id)
            : base(container)
        {
            Condition.NotDefault(id, nameof(id));

            this.inboundSerializer = inboundSerializer;
            this.outboundSerializer = outboundSerializer;
            this.cts = new CancellationTokenSource();
            this.socket = new RouterSocket()
            {
                Options =
                {
                    Linger = TimeSpan.Zero,
                    Identity = Encoding.Unicode.GetBytes(id.ToString()),
                },
            };

            this.ServerAddress = new ZmqServerAddress(host, port);
            this.ReceivedCount = 0;
            this.SentCount = 0;

            this.RegisterUnhandled(this.UnhandledRequest);
        }

        /// <summary>
        /// Gets the server address for the router.
        /// </summary>
        public ZmqServerAddress ServerAddress { get; }

        /// <summary>
        /// Gets the server received message count.
        /// </summary>
        public int ReceivedCount { get; private set; }

        /// <summary>
        /// Gets the server processed message count.
        /// </summary>
        public int SentCount { get; private set; }

        /// <inheritdoc />
        protected override void OnStart(Start start)
        {
            this.socket.Bind(this.ServerAddress.Value);
            this.Log.Debug($"Bound router socket to {this.ServerAddress}");

            this.isReceiving = true;
            Task.Run(this.StartWork, this.cts.Token);
        }

        /// <inheritdoc />
        protected override void OnStop(Stop stop)
        {
            this.isReceiving = false;
            this.cts.Cancel();
            this.socket.Unbind(this.ServerAddress.Value);
            this.Log.Debug($"Unbound router socket from {this.ServerAddress}");

            this.socket.Dispose();
        }

        /// <summary>
        /// Sends a message with the given payload to the given receiver identity address.
        /// </summary>
        /// <param name="receiverId">The receiver identity.</param>
        /// <param name="outbound">The outbound message to send.</param>
        protected void SendMessage(byte[] receiverId, Response outbound)
        {
            var message = new NetMQMessage(3);
            message.Append(receiverId);
            message.AppendEmptyFrame(); // Delimiter
            message.Append(this.outboundSerializer.Serialize(outbound));

            this.socket.SendMultipartMessage(message);

            this.SentCount++;
            this.Log.Verbose($"Sent message[{this.SentCount}] {outbound}");
        }

        /// <summary>
        /// Sends a MessageRejected to the given receiver identity address.
        /// </summary>
        /// <param name="receiverId">The receiver identity.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="message">The rejected message.</param>
        protected void SendRejected(
            byte[] receiverId,
            Guid correlationId,
            string message)
        {
            var rejected = new MessageRejected(
                message,
                correlationId,
                Guid.NewGuid(),
                this.TimeNow());

            this.SendMessage(receiverId, rejected);
        }

        private Task StartWork()
        {
            while (this.isReceiving)
            {
                this.ReceiveMessage();
            }

            this.Log.Debug("Stopped receiving messages.");
            return Task.CompletedTask;
        }

        private void ReceiveMessage()
        {
            var zmqMessage = this.socket.ReceiveMultipartBytes();
            var senderId = zmqMessage[0];
            var payload = this.inboundSerializer.Deserialize(zmqMessage[2]);

            var received = new ReceivedMessage<T>(senderId, payload);

            this.SendToSelf(received);

            this.ReceivedCount++;
            this.Log.Verbose($"Received message[{this.ReceivedCount}] {received.Payload}");
        }

        /// <summary>
        /// Handle the given unhandled request message.
        /// </summary>
        /// <param name="message">The unhandled object.</param>
        private void UnhandledRequest(object message)
        {
            if (message is ReceivedMessage<T> request)
            {
                var errorMessage = $"Message type {request.Payload.Type.Name} not valid on this port";
                this.SendRejected(request.SenderId, request.Payload.Id, errorMessage);
                this.Log.Error(errorMessage);
            }

            this.AddToUnhandledMessages(message);
        }
    }
}