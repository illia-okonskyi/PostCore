using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace PostCore.Core.Tests
{
    public class UrlExtensionsTest
    {
        [Fact]
        public void PathAndQuery()
        {
            var path = "/home/index";
            var query = "?query=query";

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.Request.Path)
                .Returns(path);
            httpContextMock.Setup(m => m.Request.QueryString)
                .Returns(new QueryString(query));
            var httpContext = httpContextMock.Object;

            Assert.Equal(path + query, httpContext.Request.PathAndQuery());
        }
    }
}
