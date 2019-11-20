using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using PostCore.Utils;

namespace PostCore.ViewUtils.TagHelpers
{
    [HtmlTargetElement("div", Attributes = "list-options,pagination-info")]
    public class PagesDivTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        public PagesDivTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public string Controller { get; set; }
        public string Action { get; set; }
        public ListOptions ListOptions { get; set; }
        public PaginationInfo PaginationInfo { get; set; }
        public string ListOptionsRouteKey { get; set; } = "options";

        public string PageClass { get; set; }
        public string PageClassNormal { get; set; }
        public string PageClassCurrent { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var result = new TagBuilder("div");
            var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

            var minPage = PaginationInfo.CurrentPage - 2;
            var maxPage = PaginationInfo.CurrentPage + 2;
            if (minPage < 1)
            {
                maxPage = Math.Min(PaginationInfo.TotalPages, maxPage + Math.Abs(minPage) + 1);
                minPage = 1;
            }
            if (maxPage > PaginationInfo.TotalPages)
            {
                minPage = Math.Max(1, minPage - Math.Abs(maxPage - PaginationInfo.TotalPages));
                maxPage = PaginationInfo.TotalPages;
            }

            result.InnerHtml.AppendHtml(MakePageAnchor(urlHelper, 1, true, "<<"));
            result.InnerHtml.Append("\n");
            result.InnerHtml.AppendHtml(MakePageAnchor(
                urlHelper,
                PaginationInfo.CurrentPage - 1,
                PaginationInfo.HasPreviousPage,
                "<"));
            result.InnerHtml.Append("\n");

            for (long i = minPage; i <= maxPage; ++i)
            {
                if (i == PaginationInfo.CurrentPage)
                {
                    result.InnerHtml.AppendHtml(MakeCurrentPageSpan(i));
                    result.InnerHtml.Append("\n");
                }
                else
                {
                    result.InnerHtml.AppendHtml(MakePageAnchor(urlHelper, i, true));
                    result.InnerHtml.Append("\n");
                }
            }

            result.InnerHtml.AppendHtml(MakePageAnchor(
                urlHelper,
                PaginationInfo.CurrentPage + 1,
                PaginationInfo.HasNextPage,
                ">"));
            result.InnerHtml.Append("\n");
            result.InnerHtml.AppendHtml(MakePageAnchor(
                urlHelper,
                PaginationInfo.TotalPages,
                true,
                ">>"));

            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.Clear();
            output.Content.AppendHtml(result.InnerHtml);
        }

        private IHtmlContent MakeCurrentPageSpan(long page)
        {
            var tag = new TagBuilder("span");
            tag.InnerHtml.Append(page.ToString());
            tag.AddCssClass(PageClass);
            tag.AddCssClass(PageClassCurrent);
            return tag;
        }

        private IHtmlContent MakePageAnchor(
            IUrlHelper urlHelper,
            long page,
            bool enabled,
            string displayName = null)
        {
            var listOptions = ListOptions ?? new ListOptions();
            var newListOptions = new ListOptions
            {
                Filters = listOptions.Filters,
                SortKey = listOptions.SortKey,
                SortOrder = listOptions.SortOrder,
                Page = page
            };

            var href = urlHelper.Action(new UrlActionContext
            {
                Action = Action ?? ViewContext.RouteData.Values["action"].ToString(),
                Controller = Controller ?? ViewContext.RouteData.Values["controller"].ToString(),
                Values = new Dictionary<string, object>
                {
                    { ListOptionsRouteKey, newListOptions }
                }
            });

            var tag = new TagBuilder("a");
            tag.InnerHtml.Append(displayName ?? page.ToString());
            tag.Attributes["href"] = href;
            tag.AddCssClass(PageClass);
            tag.AddCssClass(PageClassNormal);
            if (!enabled)
            {
                tag.AddCssClass("disabled");
            }
            return tag;
        }
    }
}
