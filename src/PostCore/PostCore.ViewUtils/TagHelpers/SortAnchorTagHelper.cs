using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using PostCore.Utils;

namespace PostCore.ViewUtils.TagHelpers
{
    [HtmlTargetElement("sorta")]
    public class SortAnchorTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        public SortAnchorTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public string Controller { get; set; }
        public string Action { get; set; }
        public string DisplayName { get; set; }
        public ListOptions ListOptions { get; set; }
        public string ListOptionsRouteKey { get; set; } = "options";
        public string SortKey { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "a";
            output.TagMode = TagMode.StartTagAndEndTag;

            var currentSortOrder = SortOrder.NoSort;
            var newSortOrder = SortOrder.Ascending;
            if (ListOptions != null && SortKey == ListOptions.SortKey)
            {
                currentSortOrder = ListOptions.SortOrder;
                if (currentSortOrder == SortOrder.Ascending)
                {
                    newSortOrder = SortOrder.Descending;
                }
            }

            var content = DisplayName;
            switch (currentSortOrder)
            {
                case SortOrder.Ascending:
                    content += " ▼";
                    break;
                case SortOrder.Descending:
                    content += " ▲";
                    break;
            }
            output.Content.SetContent(content);

            var newListOptions = new ListOptions
            {
                Filters = ListOptions?.Filters ?? new Dictionary<string, string>(),
                SortKey = SortKey,
                SortOrder = newSortOrder,
                Page = 1
            };

            var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
            var href = urlHelper.Action(new UrlActionContext
            {
                Action = Action ?? ViewContext.RouteData.Values["action"].ToString(),
                Controller = Controller ?? ViewContext.RouteData.Values["controller"].ToString(),
                Values = new Dictionary<string, object>
                {
                    { ListOptionsRouteKey, newListOptions }
                }
            });
            output.Attributes.SetAttribute("href", href);
        }
    }
}
