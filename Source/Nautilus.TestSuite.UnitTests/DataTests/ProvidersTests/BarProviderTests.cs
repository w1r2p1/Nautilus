// -------------------------------------------------------------------------------------------------
// <copyright file="BarProviderTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  https://nautechsystems.io
//
//  Licensed under the GNU Lesser General Public License Version 3.0 (the "License");
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at https://www.gnu.org/licenses/lgpl-3.0.en.html
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.DataTests.ProvidersTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Common.Interfaces;
    using Nautilus.Data.Interfaces;
    using Nautilus.Data.Keys;
    using Nautilus.Data.Messages.Requests;
    using Nautilus.Data.Messages.Responses;
    using Nautilus.Data.Providers;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Network;
    using Nautilus.Network.Messages;
    using Nautilus.Serialization.DataSerializers;
    using Nautilus.TestSuite.TestKit.Components;
    using Nautilus.TestSuite.TestKit.Fixtures;
    using Nautilus.TestSuite.TestKit.Mocks;
    using Nautilus.TestSuite.TestKit.Stubs;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class BarProviderTests : NetMQTestBase
    {
        private readonly IComponentryContainer container;
        private readonly IMessageBusAdapter messagingAdapter;
        private readonly IBarRepository repository;
        private readonly IDataSerializer<Bar> dataSerializer;

        public BarProviderTests(ITestOutputHelper output)
            : base(output)
        {
            // Fixture Setup
            this.container = TestComponentryContainer.Create(output);
            this.messagingAdapter = new MockMessageBusProvider(this.container).Adapter;
            this.dataSerializer = new BarDataSerializer();
            this.repository = new MockBarRepository(this.dataSerializer);
        }

        [Fact]
        internal void GivenBarDataRequest_WithNoBars_ReturnsQueryFailedMessage()
        {
            // Arrange
            var provider = new BarProvider(
                this.container,
                this.messagingAdapter,
                this.repository,
                this.dataSerializer);
            provider.Start().Wait();

            var barType = StubBarType.AUDUSD_OneMinuteAsk();

            var query = new Dictionary<string, string>
            {
                { "DataType", "Bar[]" },
                { "Symbol", barType.Symbol.Value },
                { "Specification", barType.Specification.ToString() },
                { "FromDate", new DateKey(StubZonedDateTime.UnixEpoch()).ToString() },
                { "ToDate", new DateKey(StubZonedDateTime.UnixEpoch()).ToString() },
                { "Limit", "0" },
            };

            var request = new DataRequest(
                query,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            var response = provider.FindData(request);

            // Assert
            Assert.Equal(typeof(QueryFailure), response.Type);
        }

        [Fact]
        internal void GivenBarDataRequest_WithBars_ReturnsValidBarDataResponse()
        {
            // Arrange
            var testAddress = new ZmqNetworkAddress(NetworkAddress.LocalHost, new Port(55524));

            var provider = new BarProvider(
                this.container,
                this.messagingAdapter,
                this.repository,
                this.dataSerializer);
            provider.Start().Wait();

            var barType = StubBarType.AUDUSD_OneMinuteAsk();
            var bar1 = StubBarProvider.Build();
            var bar2 = StubBarProvider.Build();

            this.repository.Add(barType, bar1);
            this.repository.Add(barType, bar2);

            var query = new Dictionary<string, string>
            {
                { "DataType", "Bar[]" },
                { "Symbol", barType.Symbol.Value },
                { "Specification", barType.Specification.ToString() },
                { "FromDate", new DateKey(StubZonedDateTime.UnixEpoch()).ToString() },
                { "ToDate", new DateKey(StubZonedDateTime.UnixEpoch()).ToString() },
                { "Limit", "0" },
            };

            var request = new DataRequest(
                query,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            var response = (DataResponse)provider.FindData(request);
            var bars = this.dataSerializer.DeserializeBlob(response.Data);

            // Assert
            Assert.Equal(typeof(DataResponse), response.Type);
            Assert.Equal(2, bars.Length);
            Assert.Equal(bar1, bars[0]);
            Assert.Equal(bar2, bars[1]);
        }
    }
}
