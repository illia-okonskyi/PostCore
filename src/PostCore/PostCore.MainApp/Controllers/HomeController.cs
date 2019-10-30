using Microsoft.AspNetCore.Mvc;
using PostCore.Core;

namespace PostCore.MainApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(HelloWorld.IndexText as object);
        }
    }
}
