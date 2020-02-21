// -------------------------------------------------------------------------------------------------
// <copyright file="DataPublisher{T}.cs" company="Nautech Systems Pty Ltd">
//   Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//   The use of this source code is governed by the license as found in the LICENSE.txt file.
//   https://nautechsystems.io
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.Network
{
    using System;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using Nautilus.Common.Data;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Common.Logging;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Network.Encryption;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// Provides a generic data publisher.
    /// </summary>
    /// <typeparam name="T">The publishing data type.</typeparam>
    public abstract class DataPublisher<T> : DataBusConnected, IDisposable
    {
        private readonly IDataSerializer<T> serializer;
        private readonly ICompressor compressor;
        private readonly PublisherSocket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPublisher{T}"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="dataBusAdapter">The data bus adapter.</param>
        /// <param name="serializer">The data serializer.</param>
        /// <param name="compressor">The data compressor.</param>
        /// <param name="encryption">The encryption configuration.</param>
        /// <param name="host">The publishers host address.</param>
        /// <param name="port">The publishers port.</param>
        protected DataPublisher(
            IComponentryContainer container,
            IDataBusAdapter dataBusAdapter,
            IDataSerializer<T> serializer,
            ICompressor compressor,
            EncryptionSettings encryption,
            NetworkAddress host,
            Port port)
            : base(container, dataBusAdapter)
        {
            this.serializer = serializer;
            this.compressor = compressor;

            this.socket = new PublisherSocket()
            {
                Options =
                {
                    Identity = Encoding.Unicode.GetBytes($"{nameof(Nautilus)}-{this.Name.Value}"),
                    Linger = TimeSpan.FromSeconds(1),
                },
            };

            this.NetworkAddress = new ZmqNetworkAddress(host, port);

            if (encryption.UseEncryption)
            {
                EncryptionProvider.SetupSocket(encryption, this.socket);
                this.Logger.LogInformation(LogId.Networking, $"{encryption.Algorithm} encryption setup for {this.NetworkAddress}");
            }
            else
            {
                this.Logger.LogWarning(LogId.Networking, $"No encryption setup for {this.NetworkAddress}");
            }

            this.PublishedCount = 0;
        }

        /// <summary>
        /// Gets the network address for the publisher.
        /// </summary>
        public ZmqNetworkAddress NetworkAddress { get; }

        /// <summary>
        /// Gets the server received message count.
        /// </summary>
        public int PublishedCount { get; private set; }

        /// <summary>
        /// Dispose of the socket.
        /// </summary>
        public void Dispose()
        {
            if (this.ComponentState == ComponentState.Running)
            {
                throw new InvalidOperationException("Cannot dispose a running component.");
            }

            if (!this.socket.IsDisposed)
            {
                this.socket.Dispose();
            }
        }

        /// <inheritdoc />
        protected override void OnStart(Start start)
        {
            this.socket.Bind(this.NetworkAddress.Value);
            this.Logger.LogInformation(LogId.Networking, $"Bound {this.socket.GetType().Name} to {this.NetworkAddress}");
        }

        /// <inheritdoc />
        protected override void OnStop(Stop stop)
        {
            this.socket.Unbind(this.NetworkAddress.Value);
            this.Logger.LogInformation(LogId.Networking, $"Unbound {this.socket.GetType().Name} from {this.NetworkAddress}");
        }

        /// <summary>
        /// Sends the given message onto the socket.
        /// </summary>
        /// <param name="topic">The messages topic.</param>
        /// <param name="message">The message to publish.</param>
        protected void Publish(string topic, T message)
        {
            var publishable = this.compressor.Compress(this.serializer.Serialize(message));
            this.socket.SendMultipartBytes(Encoding.UTF8.GetBytes(topic), publishable);

            this.PublishedCount++;
            this.Logger.LogTrace(LogId.Networking, $"[{this.PublishedCount}]--> Topic={topic}, Message={message}");
        }
    }
}
