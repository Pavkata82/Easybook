using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Easybook.Constants;
using Easybook.Data;
using Microsoft.EntityFrameworkCore;
using Easybook.Areas.Admin.Models.ViewModels;

namespace Easybook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)] // Restrict access to Admins only
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ManageUsers()
        {
            return View();
        }
    }
}
