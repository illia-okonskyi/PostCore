using Xunit;

namespace PostCore.Utils.Tests
{
    public class DummyTest
    {
        [Fact]
        public void CheckHelloWorld()
        {
            Assert.Equal("Hello, world, PostCore.Utils", Dummy.HelloWorld);
        }
    }
}
