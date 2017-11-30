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
        public void TestAllComponents()
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
        public void TestNameWithSpaceAndAge()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("ders Karl 24", 1, out parsedRequest, out error);

            Assert.True(isOk);
            Assert.Equal(parsedRequest.name, "ders Karl");
            Assert.Null(parsedRequest.phone);
            Assert.Equal(parsedRequest.age, 24);
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

        [Fact]
        public void TestPhone160()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("160", 1, out parsedRequest, out error);

            Assert.True(isOk);
            Assert.Null(parsedRequest.name);
            Assert.Equal(parsedRequest.phone, "160");
            Assert.Null(parsedRequest.age);
        }

        [Fact]
        public void TestEmpty()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("", 1, out parsedRequest, out error);

            Assert.False(isOk);
        }

        [Fact]
        public void TestMix()
        {
            ParsedRequest parsedRequest;
            string error;
            var isOk = ParsedRequest.Parse("A1B2C3", 1, out parsedRequest, out error);

            Assert.True(isOk);
            Assert.Equal(parsedRequest.name, "A1B2C3");
            Assert.Null(parsedRequest.phone);
            Assert.Null(parsedRequest.age);
        }

    }
}
