using Easybook.Data;
using Easybook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Easybook.Areas.Identity.Pages.Account.Manage
{
    public class BookingsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public BookingsModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<Booking> Bookings { get; set; } = new List<Booking>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Fetch bookings related to the logged-in user
            Bookings = await _context.Bookings
                .Where(b => b.UserId == user.Id)
                .Include(b => b.Hotel)
                .Include(b => b.BookingDateRanges)
                .Include(b => b.Status)
                .ToListAsync();

            return Page();
        }
    }
}
