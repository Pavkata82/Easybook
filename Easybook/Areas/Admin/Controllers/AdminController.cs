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
            var currentYear = DateTime.Now.Year;
            var startOfYear = new DateTime(currentYear, 1, 1);  // Start of the current year at midnight
            var endOfYear = new DateTime(currentYear + 1, 1, 1).AddMilliseconds(-1);  // End of the year, 23:59:59.999

            // Fetch the bookings data, grouped by month
            var bookingsData = _context.Bookings
                .Where(b => b.DateOfBooking >= startOfYear && b.DateOfBooking < endOfYear)
                .GroupBy(b => b.DateOfBooking.Month) // Group by month
                .Select(g => new
                {
                    Month = g.Key,
                    BookingCount = g.Count()
                })
                .OrderBy(g => g.Month) // Order by month
                .ToList();

            // Ensure we have data for all months (even with 0 bookings)
            var allMonths = Enumerable.Range(1, 12).Select(month => new
            {
                Month = month,
                BookingCount = bookingsData.FirstOrDefault(b => b.Month == month)?.BookingCount ?? 0
            }).ToList();

            // Pass the data to the view
            ViewData["BookingsData"] = allMonths;

            return View();
        }

        public IActionResult ManageUsers()
        {
            return View();
        }
    }
}
