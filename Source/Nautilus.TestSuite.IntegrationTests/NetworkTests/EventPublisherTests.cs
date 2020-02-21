//--------------------------------------------------------------------------------------------------
// <copyright file="EventPublisherTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.IntegrationTests.NetworkTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading.Tasks;
    using Nautilus.Common.Interfaces;
    using Nautilus.DomainModel.Events;
    using Nautilus.DomainModel.Identifiers;
    using Nautilus.Execution.Network;
    using Nautilus.Messaging.Interfaces;
    using Nautilus.Network;
    using Nautilus.Network.Compression;
    using Nautilus.Network.Encryption;
    using Nautilus.Serialization.MessageSerializers;
    using Nautilus.TestSuite.TestKit;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using NetMQ;
    using NetMQ.Sockets;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class EventPublisherTests : IDisposable
    {
        private readonly NetworkAddress localHost = new NetworkAddress("127.0.0.1");
        private readonly ITestOutputHelper output;
        private readonly IComponentryContainer container;
        private readonly MockLogger logger;
        private readonly IMessageBusAdapter messageBusAdapter;
        private readonly IEndpoint receiver;

        public EventPublisherTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.output = output;

            var containerFactory = new StubComponentryContainerProvider();
            this.container = containerFactory.Create();
            this.logger = containerFactory.Logger;
            var service = new MockMessageBusProvider(this.container);
            this.messageBusAdapter = service.Adapter;
            this.receiver = new MockMessagingAgent().Endpoint;
        }

        public void Dispose()
        {
            NetMQConfig.Cleanup(false);
        }

        [Fact]
        internal void Test_can_publish_events()
        {
            // Arrange
            const string testAddress = "tcp://127.0.0.1:56601";

            var publisher = new EventPublisher(
                this.container,
                new MsgPackEventSerializer(),
                new CompressorBypass(),
                EncryptionSettings.None(),
                new Port(56601));
            publisher.Start();
            Task.Delay(100).Wait(); // Allow publisher to start

            var subscriber = new SubscriberSocket(testAddress);
            subscriber.Connect(testAddress);
            subscriber.Subscribe("Events:Trade:TESTER-001");

            Task.Delay(100).Wait(); // Allow socket to subscribe

            var serializer = new MsgPackEventSerializer();
            var order = new StubOrderBuilder().BuildMarketOrder();
            var rejected = StubEventMessageProvider.OrderRejectedEvent(order);
            var tradeEvent = new TradeEvent(TraderId.FromString("TESTER-001"), rejected);

            // Act
            publisher.Endpoint.Send(tradeEvent);
            this.output.WriteLine("Waiting for published events...");

            var topic = subscriber.ReceiveFrameBytes();
            var message = subscriber.ReceiveFrameBytes();
            var @event = serializer.Deserialize(message);

            // Assert
            Assert.Equal("Events:Trade:TESTER-001", Encoding.UTF8.GetString(topic));
            Assert.Equal(typeof(OrderRejected), @event.GetType());

            // Tear Down
            LogDumper.DumpWithDelay(this.logger, this.output);
            subscriber.Disconnect(testAddress);
            subscriber.Dispose();
            publisher.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            publisher.Dispose();
        }
    }
}
