﻿//--------------------------------------------------------------------------------------------------
// <copyright file="StubBarData.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.TestKit.TestDoubles
{
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.DomainModel.ValueObjects;
    using NodaTime;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public static class StubBarData
    {
        public static Bar Create(int offsetMinutes = 0)
        {
            return new Bar(
                Price.Create(1.00000m),
                Price.Create(1.00000m),
                Price.Create(1.00000m),
                Price.Create(1.00000m),
                Volume.Create(1000000),
                StubZonedDateTime.UnixEpoch() + Duration.FromMinutes(offsetMinutes));
        }

        public static Bar Create(Duration offset)
        {
            return new Bar(
                Price.Create(1.00000m),
                Price.Create(1.00000m),
                Price.Create(1.00000m),
                Price.Create(1.00000m),
                Volume.Create(1000000),
                StubZonedDateTime.UnixEpoch() + offset);
        }
    }
}
