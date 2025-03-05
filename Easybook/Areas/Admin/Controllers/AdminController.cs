using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Easybook.Constants;
using Easybook.Data;
using Microsoft.EntityFrameworkCore;
using Easybook.Areas.Admin.Models.ViewModels;
using Easybook.Models;

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
            var startOfYear = new DateTime(currentYear, 1, 1);
            var endOfYear = new DateTime(currentYear + 1, 1, 1).AddMilliseconds(-1);

            // Group bookings by Booking Date (DateOfBooking)
            var bookingsData = _context.Bookings
                .Where(b => b.DateOfBooking >= startOfYear && b.DateOfBooking < endOfYear)
                .GroupBy(b => b.DateOfBooking.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    BookingCount = g.Count()
                })
                .OrderBy(g => g.Month)
                .ToList();

            var allBookingMonths = Enumerable.Range(1, 12).Select(month => new
            {
                Month = month,
                BookingCount = bookingsData.FirstOrDefault(b => b.Month == month)?.BookingCount ?? 0
            }).ToList();

            // Group bookings by Check-In Date (BookingDateRange.StartDate)
            var checkInData = _context.BookingDateRanges
                .Where(b => b.StartDate >= startOfYear && b.StartDate < endOfYear)
                .GroupBy(b => b.StartDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    BookingCount = g.Count()
                })
                .OrderBy(g => g.Month)
                .ToList();

            var allCheckInMonths = Enumerable.Range(1, 12).Select(month => new
            {
                Month = month,
                BookingCount = checkInData.FirstOrDefault(b => b.Month == month)?.BookingCount ?? 0
            }).ToList();

            // Pass data to the view
            ViewData["BookingsData"] = allBookingMonths;
            ViewData["CheckInData"] = allCheckInMonths;

            return View();
        }



        public IActionResult ManageUsers()
        {
            return View();
        }
    }
}