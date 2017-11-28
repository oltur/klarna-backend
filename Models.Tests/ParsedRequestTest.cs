using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Models.Tests
{
    public class ParsedRequestTests
    {
        ITestOutputHelper output;

        public ParsedRequestTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestAge40()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("40", 1, out parsedRequest, out error);

            Assert.True(isOk);
            Assert.Null(parsedRequest.phone);
            Assert.Equal(parsedRequest.age, 40);
        }

        [Fact]
        public void Test20JohanPlus47123456789()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("20 Johan 1234-567890", 1, out parsedRequest, out error);

            Assert.True(isOk);
            Assert.Equal(parsedRequest.name, "Johan");
            Assert.Equal(parsedRequest.phone, "1234-567890");
            Assert.Equal(parsedRequest.age, 20);
        }

        [Fact]
        public void TestA()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("A", 1, out parsedRequest, out error);

            Assert.True(isOk);
            Assert.Equal(parsedRequest.name, "A");
            Assert.Null(parsedRequest.phone);
            Assert.Null(parsedRequest.age);
        }

    }
}
