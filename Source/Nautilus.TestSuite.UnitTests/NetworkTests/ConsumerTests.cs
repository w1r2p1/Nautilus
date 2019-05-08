//--------------------------------------------------------------------------------------------------
// <copyright file="ConsumerTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.NetworkTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading.Tasks;
    using Nautilus.Common.Interfaces;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Network;
    using Nautilus.TestSuite.TestKit;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using NetMQ;
    using NetMQ.Sockets;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public class ConsumerTests
    {
        private readonly ITestOutputHelper output;
        private readonly IComponentryContainer container;
        private readonly MockLoggingAdapter mockLoggingAdapter;
        private readonly MockMessagingAgent testReceiver;
        private readonly NetworkAddress localHost = NetworkAddress.LocalHost();

        public ConsumerTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.output = output;

            var containerFactory = new StubComponentryContainerFactory();
            this.container = containerFactory.Create();
            this.mockLoggingAdapter = containerFactory.LoggingAdapter;
            this.testReceiver = new MockMessagingAgent();
            this.testReceiver.RegisterHandler<byte[]>(this.testReceiver.OnMessage);
        }

        [Fact]
        internal void Test_can_receive_one_message()
        {
            // Arrange
            const string TestAddress = "tcp://127.0.0.1:5555";
            var requester = new RequestSocket(TestAddress);
            requester.Connect(TestAddress);

            var consumer = new Consumer(
                this.container,
                this.testReceiver.Endpoint,
                new Label("CommandConsumer"),
                this.localHost,
                new Port(5555),
                Guid.NewGuid());

            consumer.Start();

            // Act
            requester.SendFrame("MSG");

            Task.Delay(100).Wait();

            // Assert
            LogDumper.Dump(this.mockLoggingAdapter, this.output);
            Assert.Contains("MSG", this.testReceiver.Messages);

            // Tear Down
            requester.Disconnect(TestAddress);
            requester.Dispose();
            consumer.Stop();
        }

        [Fact]
        internal void Test_can_receive_multiple_messages()
        {
            // Arrange
            const string TestAddress = "tcp://127.0.0.1:5556";
            var requester = new RequestSocket(TestAddress);
            requester.Connect(TestAddress);

            var consumer = new Consumer(
                this.container,
                this.testReceiver.Endpoint,
                new Label("CommandConsumer"),
                this.localHost,
                new Port(5556),
                Guid.NewGuid());

            consumer.Start();

            // Act
            requester.SendFrame("MSG-1");
            var response1 = Encoding.UTF8.GetString(requester.ReceiveFrameBytes());
            requester.SendFrame("MSG-2");
            var response2 = Encoding.UTF8.GetString(requester.ReceiveFrameBytes());

            Task.Delay(100).Wait();

            // Assert
            LogDumper.Dump(this.mockLoggingAdapter, this.output);
            Assert.Contains("MSG-1", this.testReceiver.Messages);
            Assert.Contains("MSG-2", this.testReceiver.Messages);
            Assert.Equal("OK", response1);
            Assert.Equal("OK", response2);

            // Tear Down
            requester.Disconnect(TestAddress);
            requester.Dispose();
            consumer.Stop();
        }

        [Fact]
        internal void Test_can_be_stopped()
        {
            // Arrange
            const string TestAddress = "tcp://127.0.0.1:5557";
            var requester = new RequestSocket(TestAddress);
            requester.Connect(TestAddress);

            var consumer = new Consumer(
                this.container,
                this.testReceiver.Endpoint,
                new Label("CommandConsumer"),
                this.localHost,
                new Port(5557),
                Guid.NewGuid());

            consumer.Start();
            requester.SendFrame("MSG");
            requester.ReceiveFrameBytes();
            Task.Delay(100).Wait();

            // Act
            consumer.Stop();

            requester.SendFrame("AFTER-STOPPED");

            // Assert
            LogDumper.Dump(this.mockLoggingAdapter, this.output);
            Assert.Contains("MSG", this.testReceiver.Messages);
            Assert.DoesNotContain("AFTER-STOPPED", this.testReceiver.Messages);

            // Tear Down
            requester.Disconnect(TestAddress);
            requester.Dispose();
        }

        [Fact]
        internal void Test_can_receive_one_thousand_messages_in_order()
        {
            // Arrange
            const string TestAddress = "tcp://127.0.0.1:5558";
            var requester = new RequestSocket(TestAddress);
            requester.Connect(TestAddress);

            var consumer = new Consumer(
                this.container,
                this.testReceiver.Endpoint,
                new Label("CommandConsumer"),
                this.localHost,
                new Port(5558),
                Guid.NewGuid());

            consumer.Start();

            // Act
            for (var i = 0; i < 1000; i++)
            {
                requester.SendFrame($"MSG-{i}");
                requester.ReceiveFrameBytes();
            }

            Task.Delay(100).Wait();

            // Assert
            LogDumper.Dump(this.mockLoggingAdapter, this.output);
            Assert.Equal(1000, this.testReceiver.Messages.Count);
            Assert.Equal("MSG-999", this.testReceiver.Messages[this.testReceiver.Messages.Count - 1]);
            Assert.Equal("MSG-998", this.testReceiver.Messages[this.testReceiver.Messages.Count - 2]);

            // Tear Down
            requester.Disconnect(TestAddress);
            requester.Dispose();
            consumer.Stop();
        }

        [Fact]
        internal void Test_can_receive_one_thousand_messages_from_multiple_request_sockets()
        {
            // Arrange
            const string TestAddress = "tcp://127.0.0.1:5559";
            var requester1 = new RequestSocket(TestAddress);
            var requester2 = new RequestSocket(TestAddress);
            requester1.Connect(TestAddress);
            requester2.Connect(TestAddress);

            var consumer = new Consumer(
                this.container,
                this.testReceiver.Endpoint,
                new Label("CommandConsumer"),
                this.localHost,
                new Port(5559),
                Guid.NewGuid());

            consumer.Start();

            // Act
            for (var i = 0; i < 1000; i++)
            {
                requester1.SendFrame($"MSG-{i} from 1");
                requester2.SendFrame($"MSG-{i} from 2");
                requester1.ReceiveFrameBytes();
                requester2.ReceiveFrameBytes();
            }

            Task.Delay(100).Wait();

            // Assert
            LogDumper.Dump(this.mockLoggingAdapter, this.output);
            Assert.Equal(2000, this.testReceiver.Messages.Count);
            Assert.Equal("MSG-999 from 2", this.testReceiver.Messages[this.testReceiver.Messages.Count - 1]);
            Assert.Equal("MSG-999 from 1", this.testReceiver.Messages[this.testReceiver.Messages.Count - 2]);
            Assert.Equal("MSG-998 from 2", this.testReceiver.Messages[this.testReceiver.Messages.Count - 3]);
            Assert.Equal("MSG-998 from 1", this.testReceiver.Messages[this.testReceiver.Messages.Count - 4]);

            // Tear Down
            requester1.Disconnect(TestAddress);
            requester2.Disconnect(TestAddress);
            requester1.Dispose();
            requester2.Dispose();
            consumer.Stop();
        }
    }
}