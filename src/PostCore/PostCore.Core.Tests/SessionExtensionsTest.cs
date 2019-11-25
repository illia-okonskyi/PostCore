using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace PostCore.Core.Tests
{
    public class SessionExtensionsTest
    {
        class Context
        {
            public ISession Session { get; set; }
            public Dictionary<string, byte[]> SessionObjects { get; set; } = new Dictionary<string, byte[]>();
            public delegate void TryGetValueCallback(string key, out byte[] value);
        }

        Context MakeContext()
        {
            var context = new Context();

            var sessionMock = new Mock<ISession>();
            sessionMock.Setup(m => m.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback((string key, byte[] value) =>
                {
                    context.SessionObjects.Add(key, value);
                });

            var tryGetValueCallback = new Context.TryGetValueCallback((string key, out byte[] value) =>
            {
                value = context.SessionObjects[key];
            });
            byte[] dummy;
            sessionMock.Setup(m => m.TryGetValue(It.IsAny<string>(), out dummy))
                .Callback(tryGetValueCallback)
                .Returns(true);
            context.Session = sessionMock.Object;

            return context;
        }

        class JsonObjectInternal
        {
            public int IntValue { get; set; }
        }

        class JsonObject
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
            public bool BoolValue { get; set; }
            public JsonObjectInternal ObjectValue { get; set; } = new JsonObjectInternal();
            public List<int> ArrayValue { get; set; } = new List<int>();
        }

        [Fact]
        public void SetJson()
        {
            var o = new JsonObject
            {
                IntValue = 1,
                StringValue = "string",
                BoolValue = true,
                ObjectValue = new JsonObjectInternal
                {
                    IntValue = 2
                },
                ArrayValue = new List<int> { 1, 2, 3 }
            };
            var key = "jsonObject";
            var context = MakeContext();

            context.Session.SetJson(key, o);

            var expectedBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o));
            Assert.Equal(expectedBytes, context.SessionObjects[key]);
        }

        [Fact]
        public void GetJson()
        {
            var expectedO = new JsonObject
            {
                IntValue = 1,
                StringValue = "string",
                BoolValue = true,
                ObjectValue = new JsonObjectInternal
                {
                    IntValue = 2
                },
                ArrayValue = new List<int> { 1, 2, 3 }
            };
            var key = "jsonObject";
            var context = MakeContext();
            context.SessionObjects[key] = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(expectedO));

            var actualO = context.Session.GetJson<JsonObject>(key);

            Assert.Equal(expectedO.IntValue, actualO.IntValue);
            Assert.Equal(expectedO.StringValue, actualO.StringValue);
            Assert.Equal(expectedO.BoolValue, actualO.BoolValue);
            Assert.Equal(expectedO.ObjectValue.IntValue, actualO.ObjectValue.IntValue);
            Assert.Equal(expectedO.ArrayValue, actualO.ArrayValue);
        }
    }
}
