// SPDX-License-Identifier: MIT
// Copyright Mustache.MSBuild (https://github.com/skrysmanski/Mustache.MSBuild)

using System.Runtime.Serialization;

using Mustache.MSBuild.Utils;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Mustache.MSBuild.Tests.Utils;

public sealed class DataFileJsonReaderTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DataFileJsonReaderTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    #region Typed deserialization

    [Fact]
    public void Test_DeserializeObject_Typed()
    {
        // Setup
        const string TEST_JSON = "{ \"SomeValue\": 42, \"ASampleString\": \"abc\" }";

        // Test
        var testDataObject = DataFileJsonReader.DeserializeObject<TestDataObject>(TEST_JSON, dataFileName: "MyFile.txt.json");

        // Verify
        testDataObject.ShouldNotBeNull();
        testDataObject.SomeValue.ShouldBe(42);
        testDataObject.ASampleString.ShouldBe("abc");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void Test_DeserializeObject_Typed_EmptyInput(string input)
    {
        // Test
        var ex = Should.Throw<ErrorMessageException>(() => DataFileJsonReader.DeserializeObject<TestDataObject>(input, dataFileName: "MyFile.txt.json"));

        // Verify
        ex.Message.ShouldBe("The data file 'MyFile.txt.json' is empty.");
    }

    [Theory]
    [InlineData("{ \"SomeValue\": 42,")]
    [InlineData("[ 42, 43 ]")]
    public void Test_DeserializeObject_Typed_InvalidJson(string input)
    {
        // Test
        var ex = Should.Throw<ErrorMessageException>(() => DataFileJsonReader.DeserializeObject<TestDataObject>(input, dataFileName: "MyFile.txt.json"));

        // Just for debugging purposes
        this._testOutputHelper.WriteLine($"Exception text: {ex.Message}");

        // Verify
        ex.Message.ShouldStartWith("The content of data file 'MyFile.txt.json' is invalid:");
    }

    #endregion Typed deserialization

    #region Anonymous deserialization

    [Fact]
    public void Test_DeserializeObject_Anonymous()
    {
        // Setup
        const string TEST_JSON = "{ \"SomeValue\": 42, \"ASampleString\": \"abc\" }";

        // Test
        var testDataObject = DataFileJsonReader.DeserializeObject(TEST_JSON, dataFileName: "MyFile.txt.json");

        // Verify
        testDataObject.ShouldNotBeNull();
        testDataObject["SomeValue"].ShouldBe(42);
        testDataObject["ASampleString"].ShouldBe("abc");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void Test_DeserializeObject_Anonymous_EmptyInput(string input)
    {
        // Test
        var ex = Should.Throw<ErrorMessageException>(() => DataFileJsonReader.DeserializeObject(input, dataFileName: "MyFile.txt.json"));

        // Verify
        ex.Message.ShouldBe("The data file 'MyFile.txt.json' is empty.");
    }

    [Fact]
    public void Test_DeserializeObject_Anonymous_InvalidJson()
    {
        // Test
        var ex = Should.Throw<ErrorMessageException>(() => DataFileJsonReader.DeserializeObject("{ \"SomeValue\": 42,", dataFileName: "MyFile.txt.json"));

        // Just for debugging purposes
        this._testOutputHelper.WriteLine($"Exception text: {ex.Message}");

        // Verify
        ex.Message.ShouldStartWith("The content of data file 'MyFile.txt.json' is invalid:");
    }

    [Fact]
    public void Test_DeserializeObject_Anonymous_InvalidJson_Array()
    {
        // Test
        var ex = Should.Throw<ErrorMessageException>(() => DataFileJsonReader.DeserializeObject("[ 42, 43 ]", dataFileName: "MyFile.txt.json"));

        // Just for debugging purposes
        this._testOutputHelper.WriteLine($"Exception text: {ex.Message}");

        // Verify
        ex.Message.ShouldStartWith("The content of data file 'MyFile.txt.json' is not an object but a JArray.");
    }

    #endregion Anonymous deserialization

    [DataContract]
    private sealed class TestDataObject
    {
        [DataMember]
        public int SomeValue { get; }

        [DataMember]
        public string ASampleString { get; }

        public TestDataObject(int someValue, string aSampleString)
        {
            this.SomeValue = someValue;
            this.ASampleString = aSampleString;
        }
    }
}
