﻿//--------------------------------------------------------------------------------------------------
// <copyright file="QueryResultTests.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2020 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  https://nautechsystems.io
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.TestSuite.UnitTests.CoreTests.CQSTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Nautilus.Core.CQS;
    using Xunit;

    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test Suite")]
    public sealed class QueryResultTests
    {
        [Fact]
        public void Ok_WithGenericResult_ReturnsOk()
        {
            // Arrange
            var testClass = new TestClass();

            // Act
            var result = QueryResult<TestClass>.Ok(testClass);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(testClass, result.Value);
        }

        [Fact]
        public void Fail_GenericWithValueInputs_ReturnsExpectedResult()
        {
            // Arrange
            // Act
            var result = QueryResult<TestClass>.Fail("error message");

            // Assert
            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Equal("error message", result.Message);
        }

        [Fact]
        public void ActionInvoke_AttemptingToAccessValueWithNoValue_Throws()
        {
            // Arrange
            var result = QueryResult<TestClass>.Fail("error message");

            // Act
            // Assert
            Assert.True(result.IsFailure);
            Assert.Throws<InvalidOperationException>(() => ((Action)(() => { TestClass testClass = result.Value; })).Invoke());
        }

        // Only instantiated within this class for testing purposes.
        private sealed class TestClass
        {
        }
    }
}
