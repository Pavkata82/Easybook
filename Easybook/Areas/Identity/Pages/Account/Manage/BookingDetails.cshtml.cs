using Easybook.Data;
using Easybook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Easybook.Areas.Identity.Pages.Account.Manage
{
    public class BookingDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BookingDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Booking Booking { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Booking = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Status)
                .Include(b => b.BookingDateRanges)
                    .ThenInclude(d => d.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (Booking == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
