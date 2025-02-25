using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Easybook.Constants;

namespace Easybook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)] // Restrict access to Admins only
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ManageUsers()
        {
            return View();
        }

        public IActionResult ManageBookings()
        {
            return View();
        }
    }
}
