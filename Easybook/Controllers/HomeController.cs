using Easybook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Easybook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Optionally, set a ViewData message if there are any validation errors stored in TempData
            ViewData["ErrorMessages"] = TempData["ErrorMessages"];
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
