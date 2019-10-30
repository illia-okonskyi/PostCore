using Microsoft.AspNetCore.Mvc;

namespace PostCore.ViewUtils.ViewComponents
{
    public class DummyViewModel
    {
        public string Header { get; set; }
        public string Message { get; set; }
    }

    public class Dummy : ViewComponent
    {
        public IViewComponentResult Invoke(string header, string message)
        {
            return View(
                new DummyViewModel
                {
                    Header = header,
                    Message = message
                }
            );
        }
    }
}
