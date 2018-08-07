﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MarketDataProcessorTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

#pragma warning disable 169
namespace Nautilus.TestSuite.UnitTests.BlackBoxTests.DataTests.MarketTests
{
    using System.Diagnostics.CodeAnalysis;
    using Akka.Actor;
    using Nautilus.Common.MessageStore;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.NamingRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "*", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public class MarketDataProcessorTests
    {
        private readonly ITestOutputHelper output;
        private readonly IActorRef marketDataProcessorRef;
        private readonly MockLoggingAdapter mockLoggingAdapter;
        private readonly InMemoryMessageStore inMemoryMessageStore;
        private readonly Symbol symbol;

        public MarketDataProcessorTests(ITestOutputHelper output)
        {
            // Fixture Setup
            this.output = output;

//            var setupFactory = new StubSetupContainerFactory();
//            var setupContainer = setupFactory.Create();
//            this.mockLoggingAdatper = setupFactory.LoggingAdatper;
//
//            var testActorSystem = ActorSystem.Create(nameof(MarketDataProcessorTests));
//
//            var messagingServiceFactory = new MockMessagingServiceFactory();
//            messagingServiceFactory.Create(
//                testActorSystem,
//                setupContainer);
//
//            this.inMemoryMessageStore = messagingServiceFactory.InMemoryMessageStore;
//            var messagingAdapter = messagingServiceFactory.MessagingAdapter;
//
//            this.symbol = new Symbol("AUDUSD", Exchange.LMAX);
//
//            this.marketDataProcessorRef = testActorSystem.ActorOf(Props.Create(() => new BarAggregationController(
//                setupContainer,
//                messagingAdapter,
//                this.symbol)));
        }

//        [Fact]
//        internal void GivenSubscribeSymbolDataTypeMessage_WithMinutebarTypeification_SetsUpBarAggregator()
//        {
//            // Arrange
//            var barTypeification = new barTypeification(BarQuoteType.Bid, BarResolution.Minute, 5);
//            var tradeType = new TradeType("TestScalp");
//            var message = new SubscribeSymbolbarType(
//                this.symbol,
//                barTypeification,
//                tradeType,
//                0.00001m,
//                Guid.NewGuid(),
//                StubZonedDateTime.UnixEpoch()()());
//
//            // Act
//            this.marketDataProcessorRef.Tell(message);
//
//            // Assert
//            LogDumper.Dump(this.mockLoggingAdatper, this.output);
//
//            CustomAssert.EventuallyContains(
//                "MarketDataProcessor-AUDUSD.LMAX: Initializing...",
//                this.mockLoggingAdatper,
//                EventuallyContains.TimeoutMilliseconds,
//                EventuallyContains.PollIntervalMilliseconds);
//
//            CustomAssert.EventuallyContains(
//                "BarAggregator-AUDUSD.LMAX-5-Minute[Bid]: Initializing...",
//                this.mockLoggingAdatper,
//                EventuallyContains.TimeoutMilliseconds,
//                EventuallyContains.PollIntervalMilliseconds);
//        }
//
//        [Fact]
//        internal void GivenSubscribeSymbolDataTypeMessage_WithTickbarTypeification_SetsUpBarAggregator()
//        {
//            // Arrange
//            var barTypeification = new barTypeification(BarQuoteType.Bid, BarResolution.Tick, 1000);
//            var tradeType = new TradeType("TestScalp");
//            var message = new SubscribeSymbolbarType(
//                this.symbol,
//                barTypeification,
//                tradeType,
//                0.00001m,
//                Guid.NewGuid(),
//                StubZonedDateTime.UnixEpoch()()());
//
//            // Act
//            this.marketDataProcessorRef.Tell(message);
//
//            // Assert
//            LogDumper.Dump(this.mockLoggingAdatper, this.output);
//
//            CustomAssert.EventuallyContains(
//                "MarketDataProcessor-AUDUSD.LMAX: Setup for 1000-Tick[Bid] bars",
//                this.mockLoggingAdatper,
//                EventuallyContains.TimeoutMilliseconds,
//                EventuallyContains.PollIntervalMilliseconds);
//
//            CustomAssert.EventuallyContains(
//                "BarAggregator-AUDUSD.LMAX-1000-Tick[Bid]: Initializing...",
//                this.mockLoggingAdatper,
//                EventuallyContains.TimeoutMilliseconds,
//                EventuallyContains.PollIntervalMilliseconds);
//        }
//
//        [Fact]
//        internal void GivenUnsubscribeSymbolDataTypeMessage_RemovesBarAggregator()
//        {
//            // Arrange
//            var barTypeification = new barTypeification(BarQuoteType.Bid, BarResolution.Tick, 1000);
//            var tradeType = new TradeType("TestScalp");
//            var message1 = new SubscribeSymbolbarType(
//                this.symbol,
//                barTypeification,
//                tradeType,
//                0.00001m,
//                Guid.NewGuid(),
//                StubZonedDateTime.UnixEpoch()()());
//            var message2 = new UnsubscribeSymbolbarType(
//                this.symbol,
//                tradeType,
//                Guid.NewGuid(),
//                StubZonedDateTime.UnixEpoch()()());
//
//            // Act
//            this.marketDataProcessorRef.Tell(message1);
//            this.marketDataProcessorRef.Tell(message2);
//
//            // Assert
//            LogDumper.Dump(this.mockLoggingAdatper, this.output);
//
//            CustomAssert.EventuallyContains(
//                "MarketDataProcessor-AUDUSD.LMAX: Data for AUDUSD.LMAX(TestScalp) bars deregistered",
//                this.mockLoggingAdatper,
//                EventuallyContains.TimeoutMilliseconds,
//                EventuallyContains.PollIntervalMilliseconds);
//        }
    }
}
