﻿//--------------------------------------------------------------------------------------------------
// <copyright file="MessageTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2019 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.CoreTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Common.Enums;
    using Nautilus.Common.Messages.Commands;
    using Nautilus.Common.Messages.Documents;
    using Nautilus.DomainModel.ValueObjects;
    using Nautilus.TestSuite.TestKit.TestDoubles;
    using Xunit;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed. Suppression is OK within the Test Suite.")]
    public sealed class MessageTests
    {
        [Fact]
        internal void Equal_WithDifferentMessagesOfTheSameContent_CanEquateById()
        {
            // Arrange
            var message1 = new StatusResponse(
                new Label("SomeComponent1"),
                State.Running,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            var message2 = new StatusResponse(
                new Label("SomeComponent2"),
                State.Running,
                Guid.NewGuid(),
                StubZonedDateTime.UnixEpoch());

            // Act
            var result1 = message1 == message2;
            var result2 = message1.Equals(message2);
            var result3 = message1.Equals(message1);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
            Assert.True(result3);
        }

        [Fact]
        internal void ToString_ReturnsExpectedString()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var message = new StatusRequest(guid, StubZonedDateTime.UnixEpoch());

            // Act
            var result = message.ToString();

            // Assert
            Assert.StartsWith("StatusRequest(", result);
            Assert.EndsWith(")", result);
        }

        [Fact]
        internal void ToString_WhenOverridden_ReturnsExpectedString()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var message = new StatusResponse(
                new Label("CommandBus"),
                State.Running,
                guid,
                StubZonedDateTime.UnixEpoch());

            // Act
            var result = message.ToString();

            // Assert
            Assert.StartsWith("StatusResponse(", result);
            Assert.EndsWith("-CommandBus=Running", result);
        }
    }
}
