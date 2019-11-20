using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Moq;
using PostCore.Utils;
using PostCore.ViewUtils.TagHelpers;
using Xunit;

namespace PostCore.ViewUtils.Tests.TagHelpers
{
    public class SortAnchorTagHelperTest
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
                    return ctx.Controller + "/" + ctx.Action + "?" + lo.SortKey + "-" + lo.SortOrder;
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
        public void CheckHrefAndContent()
        {
            var controller = "home";
            var action = "index";
            var displayName = "displayName";
            var sortKey1 = "sortKey1";
            var sortKey2 = "sortKey2";
            var lo = new ListOptions {
                SortKey = sortKey1,
                SortOrder = SortOrder.Ascending
            };
            var loRouteKey = "lo";
            var context = MakeContext(controller, action, loRouteKey);

            // Defaults and current sort key
            var tagHelper = new SortAnchorTagHelper(context.UrlHelperFactory)
            {
                DisplayName = displayName,
                ListOptions = lo,
                SortKey = sortKey1,
                ViewContext = context.ViewContext
            };

            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);
            var href = context.TagHelperOutput.Attributes["href"].Value;
            var content = context.TagHelperOutput.Content.GetContent();

            Assert.Equal($@"{controller}/{action}?{sortKey1}-{SortOrder.Descending}", href);
            Assert.Equal($@"{displayName} &#x25BC;", content);

            // Manual specifying the controller, action and list options route key and other sort
            // key
            tagHelper.Controller = controller;
            tagHelper.Action = action;
            tagHelper.ListOptionsRouteKey = loRouteKey;
            tagHelper.SortKey = sortKey2;
            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);
            href = context.TagHelperOutput.Attributes["href"].Value;
            content = context.TagHelperOutput.Content.GetContent();

            Assert.Equal($@"{controller}/{action}?{sortKey2}-{SortOrder.Ascending}", href);
            Assert.Equal($@"{displayName}", content);
        }
    }
}
