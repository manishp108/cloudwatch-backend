using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    public class HomeController : Controller         // Controller responsible for handling home page requests
    {
        public IActionResult Index()           // Default action method that returns the Home/Index view
        {
            return View();
        }
    }
}
