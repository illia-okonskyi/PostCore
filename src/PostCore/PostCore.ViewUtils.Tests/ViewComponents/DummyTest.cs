using Xunit;
using PostCore.ViewUtils.ViewComponents;

namespace PostCore.ViewUtils.Tests.ViewComponents
{
    public class DummyTest
    {
        [Fact]
        public void CheckHeaderAndMessage()
        {
            const string header = "header";
            const string message = "message";

            var c = new Dummy();
            var r = c.Invoke(header, message).ExtractViewModel<DummyViewModel>();
            Assert.Equal(header, r?.Header);
            Assert.Equal(message, r?.Message);
        }
    }
}
