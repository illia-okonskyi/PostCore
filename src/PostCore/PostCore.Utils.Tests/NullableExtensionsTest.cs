using System;
using System.Linq;
using Xunit;

namespace PostCore.Utils.Tests
{
    public class NullableExtensionsTest
    {
        enum TheEnum
        {
            Value1,
            Value2
        }

        [Fact]
        public void TryParse_long()
        {
            long value = 123;

            Assert.False(NullableExtensions.TryParse("agfrg", out long? parsed));
            Assert.False(parsed.HasValue);
            Assert.True(NullableExtensions.TryParse(value.ToString(), out parsed));
            Assert.True(parsed.HasValue);
            Assert.Equal(value, parsed.Value);
        }

        [Fact]
        public void TryParse_DateTime()
        {
            var value = new DateTime(2019, 11, 26);

            Assert.False(NullableExtensions.TryParse("agfrg", out DateTime? parsed));
            Assert.False(parsed.HasValue);
            Assert.True(NullableExtensions.TryParse(value.ToString(), out parsed));
            Assert.True(parsed.HasValue);
            Assert.Equal(value, parsed.Value);
        }

        [Fact]
        public void TryParse_Enum()
        {
            var value = TheEnum.Value1;

            Assert.False(NullableExtensions.TryParse("agfrg", out TheEnum? parsed));
            Assert.False(parsed.HasValue);
            Assert.True(NullableExtensions.TryParse(value.ToString(), out parsed));
            Assert.True(parsed.HasValue);
            Assert.Equal(value, parsed.Value);
        }

    }
}
