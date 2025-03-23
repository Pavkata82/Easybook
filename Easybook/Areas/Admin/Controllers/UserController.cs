using Easybook.Areas.Admin.Models.ViewModels;
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

        public IActionResult ManageUsers(string searchQuery = null)
        {
            var usersQuery = _context.Users.AsQueryable(); // Start with all users

            // If a search query is provided, filter the users by name or email
            if (!string.IsNullOrEmpty(searchQuery))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.Contains(searchQuery) ||
                    u.LastName.Contains(searchQuery) ||
                    u.Email.Contains(searchQuery)
                );
            }

            var users = usersQuery.ToList(); // Execute the query and get the list of users

            return View(users); // Pass the filtered users to the view
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

            var userEditViewModel = new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePictureUrl
            };

            return View(userEditViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel userEditViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(userEditViewModel);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userEditViewModel.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Update the user's fields
            user.FirstName = userEditViewModel.FirstName;
            user.LastName = userEditViewModel.LastName;
            user.Email = userEditViewModel.Email;
            user.PhoneNumber = userEditViewModel.PhoneNumber;


            // Handle profile picture upload if a new one is provided
            if (userEditViewModel.NewProfilePicture != null)
            {
                // Check if the user already has a profile picture and if it's not the default
                if (user.ProfilePictureUrl != "/images/users/default.jpg")
                {
                    // Delete the old profile picture from the server if it's not the default
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save the new profile picture
                var fileExtension = Path.GetExtension(userEditViewModel.NewProfilePicture.FileName);
                var fileName = $"{Guid.NewGuid()}_{fileExtension}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await userEditViewModel.NewProfilePicture.CopyToAsync(stream);
                }

                // Update the profile picture URL in the user record
                user.ProfilePictureUrl = $"/images/users/{fileName}";
            }

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync(); // Save changes to the database
                TempData["SuccessMessage"] = "Потребителят е актуализиран успешно.";
                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Възникна грешка при актуализирането на потребителя: {ex.Message}");
                return View(userEditViewModel);
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
