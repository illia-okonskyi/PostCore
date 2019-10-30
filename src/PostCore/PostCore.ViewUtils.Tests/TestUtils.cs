using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace PostCore.ViewUtils.Tests
{
    static class TestUtils
    {
        public static T ExtractViewModel<T>(this IViewComponentResult r) where T : class
        {
            return (r as ViewViewComponentResult)?.ViewData.Model as T;
        }
    }
}
