using Xunit;

namespace PostCore.Core.Tests
{
    public class TestHelloWorld
    {
        [Fact]
        public void CheckIndexText()
        {
            Assert.Equal("PostCore application index", HelloWorld.IndexText);
        }
    }
}
