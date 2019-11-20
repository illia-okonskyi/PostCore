using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;
using PostCore.Utils;
using PostCore.ViewUtils.TagHelpers;

namespace PostCore.ViewUtils.Tests.TagHelpers
{
    public class PagesDivTagHelperTest
    {
        static readonly string DefaultListOptionsRouteKey = "options";

        class Context
        {
            public IUrlHelperFactory UrlHelperFactory { get; set; }
            public TagHelperContext TagHelperContext { get; set; }
            public TagHelperOutput TagHelperOutput { get; set; }
            public ViewContext ViewContext { get; set; }
        }
        Context MakeContext(string controller, string action, string loRouteKey)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns((UrlActionContext ctx) => {
                    var routeValues = ctx.Values as Dictionary<string, object>;
                    ListOptions lo;
                    if (routeValues.ContainsKey(DefaultListOptionsRouteKey))
                    {
                        lo = routeValues[DefaultListOptionsRouteKey] as ListOptions;
                    }
                    else
                    {
                        lo = routeValues[loRouteKey] as ListOptions;
                    }
                    return ctx.Controller + "/" + ctx.Action + "?" + lo.Page;
                });

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var tagHelperContext = new TagHelperContext(
                new TagHelperAttributeList(),
                new Dictionary<object, object>(),
                "");
            var content = new Mock<TagHelperContent>();
            var tagHelperOutput = new TagHelperOutput(
                "a",
                new TagHelperAttributeList(),
                (cache, encoder) => Task.FromResult(content.Object));

            var routeData = new RouteData();
            routeData.Values.Add("controller", controller);
            routeData.Values.Add("action", action);
            var viewContext = new ViewContext
            {
                RouteData = routeData
            };

            return new Context
            {
                UrlHelperFactory = urlHelperFactory.Object,
                TagHelperContext = tagHelperContext,
                TagHelperOutput = tagHelperOutput,
                ViewContext = viewContext
            };
        }

        [Fact]
        public void CheckContent()
        {
            var controller = "home";
            var action = "index";
            var loRouteKey = "lo";
            var context = MakeContext(controller, action, loRouteKey);

            // Defaults and only one page
            var tagHelper = new PagesDivTagHelper(context.UrlHelperFactory)
            {
                ViewContext = context.ViewContext,
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = 1,
                    TotalPages = 1
                }
            };

            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);
            var content = context.TagHelperOutput.Content.GetContent();
            var expectedContent = $@"<a class="" "" href=""{controller}/{action}?1"">&lt;&lt;</a>&#xA;" +
                $@"<a class=""disabled  "" href=""{controller}/{action}?0"">&lt;</a>&#xA;" +
                $@"<span class="" "">1</span>&#xA;" +
                $@"<a class=""disabled  "" href=""{controller}/{action}?2"">&gt;</a>&#xA;" +
                $@"<a class="" "" href=""{controller}/{action}?1"">&gt;&gt;</a>";
            Assert.Equal(expectedContent, content);

            // Manual specifying the controller, action, list options route key
            tagHelper.Controller = controller;
            tagHelper.Action = action;
            tagHelper.ListOptionsRouteKey = loRouteKey;

            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);
            content = context.TagHelperOutput.Content.GetContent();
            // expectedContent must be same as previous;
            Assert.Equal(expectedContent, content);


            // Styling and more pages
            tagHelper.PaginationInfo = new PaginationInfo
            {
                CurrentPage = 5,
                TotalPages = 10,
            };
            tagHelper.PageClass = "btn";
            tagHelper.PageClassNormal = "normal";
            tagHelper.PageClassCurrent = "current";

            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);
            content = context.TagHelperOutput.Content.GetContent();
            expectedContent = $@"<a class=""normal btn"" href=""{controller}/{action}?1"">&lt;&lt;</a>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?4"">&lt;</a>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?3"">3</a>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?4"">4</a>&#xA;" +
                $@"<span class=""current btn"">5</span>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?6"">6</a>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?7"">7</a>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?6"">&gt;</a>&#xA;" +
                $@"<a class=""normal btn"" href=""{controller}/{action}?10"">&gt;&gt;</a>";
            Assert.Equal(expectedContent, content);
        }
    }
}
