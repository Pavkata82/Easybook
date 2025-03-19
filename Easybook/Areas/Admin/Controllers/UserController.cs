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
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationUser userModel)
        {
            if (!ModelState.IsValid)
            {
                return View(userModel);
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userModel.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Update only specific fields to avoid unintended overwrites
            user.FirstName = userModel.FirstName;
            user.LastName = userModel.LastName;
            user.Email = userModel.Email;
            user.PhoneNumber = userModel.PhoneNumber;

            try
            {
                _context.Update(user);
                _context.SaveChanges(); // Save changes to the database
                TempData["SuccessMessage"] = "Потребителят е актуализиран успешно.";
                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Възникна грешка при актуализиране на потребителя: {ex.Message}");
                return View(userModel);
            }
        }

        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);

            _context.SaveChanges();

            return RedirectToAction(nameof(ManageUsers));
        }

    }
}
