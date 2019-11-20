using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using Xunit;
using PostCore.ViewUtils.TagHelpers;


namespace PostCore.ViewUtils.Tests.TagHelpers
{
    public class SpanCustomValidationKeyTagHelperTest
    {
        class Context
        {
            public TagHelperContext TagHelperContext { get; set; }
            public TagHelperOutput TagHelperOutput { get; set; }
        }

        Context MakeContext()
        {
            var tagHelperContext = new TagHelperContext(
                new TagHelperAttributeList(),
                new Dictionary<object, object>(),
                "");
            var content = new Mock<TagHelperContent>();
            var tagHelperOutput = new TagHelperOutput(
                "a",
                new TagHelperAttributeList(),
                (cache, encoder) => Task.FromResult(content.Object));

            return new Context
            {
                TagHelperContext = tagHelperContext,
                TagHelperOutput = tagHelperOutput
            };
        }

        [Fact]
        public void CheckContent()
        {
            var errorKey = "errorKey";
            var errorMessage1 = "errorMessage1";
            var errorMessage2 = "errorMessage2";
            var errorMessageSeparator = "; ";
            var viewContext = new ViewContext();

            var context = MakeContext();
            var tagHelper = new SpanCustomValidationKeyTagHelper
            {
                ViewContext = viewContext,
                ValidationKey = errorKey
            };

            // No error
            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);

            var content = context.TagHelperOutput.Content.GetContent();
            Assert.True(string.IsNullOrEmpty(content));

            // Has one error
            viewContext.ModelState.AddModelError(errorKey, errorMessage1);
            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);

            content = context.TagHelperOutput.Content.GetContent();
            Assert.Equal(errorMessage1, content);

            // Has two errors
            viewContext.ModelState.AddModelError(errorKey, errorMessage2);
            tagHelper.Process(context.TagHelperContext, context.TagHelperOutput);

            content = context.TagHelperOutput.Content.GetContent();
            Assert.Equal(errorMessage1 + errorMessageSeparator + errorMessage2, content);
        }
    }
}
