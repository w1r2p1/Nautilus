//--------------------------------------------------------------------------------------------------
// <copyright file="MessageServerTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.IntegrationTests.NetworkTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Interfaces;
    using Nautilus.Network;
    using Nautilus.Network.Configuration;
    using Nautilus.Network.Messages;
    using Nautilus.Serialization.MessageSerializers;
    using Nautilus.TestSuite.TestKit;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using NetMQ;
    using NetMQ.Sockets;
    using Xunit;
    using Xunit.Abstractions;
    using Encoding = System.Text.Encoding;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class MessageServerTests : IDisposable
    {
        private readonly ITestOutputHelper output;
        private readonly IComponentryContainer container;
        private readonly MockLoggingAdapter loggingAdapter;
        private readonly MockSerializer serializer;
        private readonly MsgPackResponseSerializer responseSerializer;

        public MessageServerTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.output = output;

            var containerFactory = new StubComponentryContainerProvider();
            this.container = containerFactory.Create();
            this.loggingAdapter = containerFactory.LoggingAdapter;
            this.serializer = new MockSerializer();
            this.responseSerializer = new MsgPackResponseSerializer();
        }

        public void Dispose()
        {
            NetMQConfig.Cleanup(false);
        }

        [Fact]
        internal void InitializedServer_IsInCorrectState()
        {
            // Arrange
            // Act
            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(55555),
                Guid.NewGuid());

            // Assert
            Assert.Equal("tcp://127.0.0.1:55555", server.NetworkAddress.ToString());
            Assert.Equal(ComponentState.Initialized, server.ComponentState);
            Assert.Equal(0, server.CountReceived);
            Assert.Equal(0, server.CountSent);
        }

        [Fact]
        internal void StartedServer_IsInCorrectState()
        {
            // Arrange
            // Act
            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(55556),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            // Assert
            Assert.Equal(ComponentState.Running, server.ComponentState);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void GivenMessage_WhichIsEmptyBytes_RespondsWithMessageRejected()
        {
            // Arrange
            const int testPort = 55557;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to initiate

            var requester1 = new RequestSocket(testAddress);
            requester1.Connect(testAddress);

            // Act
            requester1.SendMultipartBytes(new byte[] { });
            var response1 = this.responseSerializer.Deserialize(requester1.ReceiveFrameBytes());

            // Assert
            Assert.Equal(typeof(MessageRejected), response1.Type);
            Assert.Equal(1, server.CountReceived);
            Assert.Equal(1, server.CountSent);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester1.Disconnect(testAddress);
            requester1.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void GivenMessage_WhichHasIncorrectFrameCount_RespondsWithMessageRejected()
        {
            // Arrange
            const int testPort = 55557;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to initiate

            var requester1 = new RequestSocket(testAddress);
            requester1.Connect(testAddress);

            // Act
            requester1.SendMultipartBytes(new byte[] { }, new byte[] { }); // Two payloads incorrect
            var response1 = this.responseSerializer.Deserialize(requester1.ReceiveFrameBytes());

            // Assert
            Assert.Equal(typeof(MessageRejected), response1.Type);
            Assert.Equal(1, server.CountReceived);
            Assert.Equal(1, server.CountSent);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester1.Disconnect(testAddress);
            requester1.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void GivenMessage_WhichIsInvalidForThisPort_RespondsWithMessageRejected()
        {
            // Arrange
            const int testPort = 55558;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            var requester1 = new RequestSocket(testAddress);
            var requester2 = new RequestSocket(testAddress);
            requester1.Connect(testAddress);
            requester2.Connect(testAddress);

            // Act
            requester1.SendFrame(Encoding.UTF8.GetBytes("WOW"));
            var response1 = this.responseSerializer.Deserialize(requester1.ReceiveFrameBytes());

            // Assert
            Assert.Equal(typeof(MessageRejected), response1.Type);
            Assert.Equal(1, server.CountReceived);
            Assert.Equal(1, server.CountSent);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester1.Disconnect(testAddress);
            requester1.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void GivenOneMessage_StoresAndSendsResponseToSender()
        {
            // Arrange
            const int testPort = 55559;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            var requester = new RequestSocket(testAddress);
            requester.Connect(testAddress);

            // Act
            var message = new MockMessage(
                "TEST",
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            requester.SendFrame(this.serializer.Serialize(message));
            var response = this.responseSerializer.Deserialize(requester.ReceiveFrameBytes());

            // Assert
            Assert.Equal(typeof(MessageReceived), response.Type);
            Assert.Equal(1, server.CountReceived);
            Assert.Equal(1, server.CountSent);
            Assert.Contains(message, server.ReceivedMessages);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester.Disconnect(testAddress);
            requester.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void GivenMultipleMessages_StoresAndSendsResponsesToSender()
        {
            // Arrange
            const int testPort = 55560;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            var requester = new RequestSocket(testAddress);
            requester.Connect(testAddress);

            // Act
            var message1 = new MockMessage(
                "TEST1",
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var message2 = new MockMessage(
                "TEST2",
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            requester.SendFrame(this.serializer.Serialize(message1));
            var response1 = this.responseSerializer.Deserialize(requester.ReceiveFrameBytes());

            requester.SendFrame(this.serializer.Serialize(message2));
            var response2 = this.responseSerializer.Deserialize(requester.ReceiveFrameBytes());

            // Assert
            Assert.Contains(message1, server.ReceivedMessages);
            Assert.Contains(message2, server.ReceivedMessages);
            Assert.Equal(typeof(MessageReceived), response1.Type);
            Assert.Equal(typeof(MessageReceived), response2.Type);
            Assert.Equal(2, server.CountReceived);
            Assert.Equal(2, server.CountSent);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester.Disconnect(testAddress);
            requester.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void ServerCanBeStopped()
        {
            // Arrange
            const int testPort = 55561;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            var requester = new RequestSocket(testAddress);
            requester.Connect(testAddress);

            var message1 = new MockMessage(
                "TEST1",
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            requester.SendFrame(this.serializer.Serialize(message1));
            var response1 = this.responseSerializer.Deserialize(requester.ReceiveFrameBytes());

            // Act
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop

            var message2 = new MockMessage(
                "AFTER-STOP",
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());
            requester.SendFrame(this.serializer.Serialize(message2));

            // Assert
            Assert.Equal(typeof(MessageReceived), response1.Type);
            Assert.Contains(message1, server.ReceivedMessages);
            Assert.DoesNotContain(message2, server.ReceivedMessages);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester.Disconnect(testAddress);
            requester.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void Given1000Messages_StoresAndSendsResponsesToSenderInOrder()
        {
            // Arrange
            const int testPort = 55562;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            var requester = new RequestSocket(testAddress);
            requester.Connect(testAddress);

            // Act
            for (var i = 0; i < 1000; i++)
            {
                var message = new MockMessage(
                    $"TEST-{i}",
                    Guid.NewGuid(),
                    StubZonedDateTime.UnixEpoch());

                requester.SendFrame(this.serializer.Serialize(message));
                this.responseSerializer.Deserialize(requester.ReceiveFrameBytes());
            }

            // Assert
            Assert.Equal(1000, server.ReceivedMessages.Count);
            Assert.Equal(1000, server.CountReceived);
            Assert.Equal(1000, server.CountSent);
            Assert.Equal("TEST-999", server.ReceivedMessages[^1].Payload);
            Assert.Equal("TEST-998", server.ReceivedMessages[^2].Payload);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester.Disconnect(testAddress);
            requester.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }

        [Fact]
        internal void Given1000Messages_FromDifferentSenders_StoresAndSendsResponsesToSendersInOrder()
        {
            // Arrange
            const int testPort = 55563;
            var testAddress = "tcp://127.0.0.1:" + testPort;

            var server = new MockMessageServer(
                this.container,
                EncryptionConfig.None(),
                NetworkAddress.LocalHost,
                new NetworkPort(testPort),
                Guid.NewGuid());
            server.Start();
            Task.Delay(100).Wait(); // Allow server to start

            var requester1 = new RequestSocket(testAddress);
            var requester2 = new RequestSocket(testAddress);
            requester1.Connect(testAddress);
            requester2.Connect(testAddress);

            // Act
            for (var i = 0; i < 1000; i++)
            {
                var message1 = new MockMessage(
                    $"TEST-{i} from 1",
                    Guid.NewGuid(),
                    StubZonedDateTime.UnixEpoch());

                var message2 = new MockMessage(
                    $"TEST-{i} from 2",
                    Guid.NewGuid(),
                    StubZonedDateTime.UnixEpoch());

                requester1.SendFrame(this.serializer.Serialize(message1));
                this.responseSerializer.Deserialize(requester1.ReceiveFrameBytes());
                requester2.SendFrame(this.serializer.Serialize(message2));
                this.responseSerializer.Deserialize(requester2.ReceiveFrameBytes());
            }

            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);

            // Assert
            Assert.Equal(2000, server.ReceivedMessages.Count);
            Assert.Equal(2000, server.CountReceived);
            Assert.Equal(2000, server.CountSent);
            Assert.Equal("TEST-999 from 2", server.ReceivedMessages[^1].Payload);
            Assert.Equal("TEST-999 from 1", server.ReceivedMessages[^2].Payload);
            Assert.Equal("TEST-998 from 2", server.ReceivedMessages[^3].Payload);
            Assert.Equal("TEST-998 from 1", server.ReceivedMessages[^4].Payload);

            // Tear Down
            LogDumper.DumpWithDelay(this.loggingAdapter, this.output);
            requester1.Disconnect(testAddress);
            requester2.Disconnect(testAddress);
            requester1.Dispose();
            requester2.Dispose();
            server.Stop();
            Task.Delay(100).Wait(); // Allow server to stop
            server.Dispose();
        }
    }
}
