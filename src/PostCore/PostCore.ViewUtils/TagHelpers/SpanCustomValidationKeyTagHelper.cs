using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PostCore.ViewUtils.TagHelpers
{
    [HtmlTargetElement("span", Attributes = "validation-key")]
    public class SpanCustomValidationKeyTagHelper : TagHelper
    {
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public string ValidationKey { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(ValidationKey))
            {
                return;
            }

            var modelState = ViewContext.ModelState;
            if (!modelState.ContainsKey(ValidationKey))
            {
                return;
            }
            var entry = modelState[ValidationKey];
            var errorString = string.Join("; ", entry.Errors.Select(e => e.ErrorMessage));
            output.Content.SetContent(errorString);
        }
    }

}
