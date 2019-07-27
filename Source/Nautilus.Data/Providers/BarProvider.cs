//--------------------------------------------------------------------------------------------------
// <copyright file="BarProvider.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Data.Providers
{
    using System;
    using System.Linq;
    using Nautilus.Common.Interfaces;
    using Nautilus.Core;
    using Nautilus.Data.Interfaces;
    using Nautilus.Data.Messages.Requests;
    using Nautilus.Data.Messages.Responses;
    using Nautilus.DomainModel.Frames;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.Messaging;
    using Nautilus.Network;

    /// <summary>
    /// Provides <see cref="Bar"/> data to requests.
    /// </summary>
    public sealed class BarProvider : MessageServer<Request, Response>
    {
        private readonly IBarRepository repository;
        private readonly IDataSerializer<BarDataFrame> dataSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BarProvider"/> class.
        /// </summary>
        /// <param name="container">The componentry container.</param>
        /// <param name="repository">The instrument repository.</param>
        /// <param name="inboundSerializer">The inbound message serializer.</param>
        /// <param name="outboundSerializer">The outbound message serializer.</param>
        /// <param name="dataSerializer">The data serializer.</param>
        /// <param name="host">The host address.</param>
        /// <param name="port">The port.</param>
        public BarProvider(
            IComponentryContainer container,
            IBarRepository repository,
            IDataSerializer<BarDataFrame> dataSerializer,
            IMessageSerializer<Request> inboundSerializer,
            IMessageSerializer<Response> outboundSerializer,
            NetworkAddress host,
            NetworkPort port)
            : base(
                container,
                inboundSerializer,
                outboundSerializer,
                host,
                port,
                Guid.NewGuid())
        {
            this.repository = repository;
            this.dataSerializer = dataSerializer;

            this.RegisterHandler<Envelope<BarDataRequest>>(this.OnMessage);
        }

        private void OnMessage(Envelope<BarDataRequest> envelope)
        {
            var request = envelope.Message;
            var barType = new BarType(request.Symbol, request.BarSpecification);
            var query = this.repository.Find(
                barType,
                request.FromDateTime,
                request.ToDateTime);

            if (query.IsFailure)
            {
                this.SendQueryFailure(query.Message, request.Id, envelope.Sender);
                this.Log.Warning($"{envelope.Message} query failed ({query.Message}).");
                return;
            }

            var bars = query
                .Value
                .Bars
                .ToArray();

            var data = this.dataSerializer.Serialize(new BarDataFrame(barType, bars));

            var response = new DataResponse(
                data,
                this.dataSerializer.DataEncoding,
                request.Id,
                this.NewGuid(),
                this.TimeNow());

            this.SendMessage(response, envelope.Sender);
        }
    }
}
