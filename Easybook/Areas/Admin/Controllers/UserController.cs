using Easybook.Constants;
using Easybook.Data;
using Easybook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Easybook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)] // Restrict access to Admins only
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult ManageUsers()
        {
            var users = _context.Users.ToList(); // Fetch all users
            return View(users);
        }

        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Hotel)  // Include Hotel details
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Status)  // Include Booking Status
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.BookingDateRanges)  // Include BookingDateRanges
                        .ThenInclude(br => br.Room) // Include Room details
                        .ThenInclude(r => r.RoomType) // Include RoomType details
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

    }
}
