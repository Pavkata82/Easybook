using Easybook.Areas.Admin.Models.ViewModels;
using Easybook.Constants;
using Easybook.Data;
using Easybook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Easybook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)] // Restrict access to Admins only
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult ManageBookings()
        {
            // Retrieve bookings from the database
            var bookings = _context.Bookings
                .Include(b => b.User) // Include related User data (if needed)
                .Include(b => b.Hotel) // Include related Hotel data (if needed)
                .Include(b => b.Status) // Include related Status data
                .Include(b => b.BookingDateRanges)
                .OrderByDescending(b => b.BookingId)
                .ToList();

            // Populate the dropdown with status options
            ViewBag.Statuses = _context.Statuses.ToList();

            return View(bookings); // Pass the data to the view
        }


        public IActionResult BookingDetails(int id)
        {
            var booking = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Hotel)
                .Include(b => b.Status)
                .Include(b => b.BookingDateRanges)
                    .ThenInclude(br => br.Room)
                .Include(b => b.BookingDateRanges)
                    .ThenInclude(br => br.Room.RoomType)
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // ✅ Fetch the available statuses from the database
            ViewBag.Statuses = _context.Statuses.ToList();

            // Prepare the ViewModel
            var bookingDetailsViewModel = new BookingDetailsViewModel
            {
                BookingId = booking.BookingId,
                CustomerName = booking.User?.UserName,
                Status = booking.Status?.Name,
                HotelName = booking.Hotel?.Name,
                HotelId = booking.Hotel?.HotelId ?? 0,
                UserId = booking.User?.Id,
                TotalPrice = booking.TotalPrice,
                SpecialRequests = booking.SpecialRequests,
                CheckInDate = booking.BookingDateRanges.Select(bdr => bdr.StartDate).First(),
                CheckOutDate = booking.BookingDateRanges.Select(bdr => bdr.EndDate).First(),
                IsPaid = booking.IsPaid,
                RoomDetails = new List<RoomDetailViewModel>()
            };

            // Fetch room types and their prices
            var roomPrices = _context.Rooms
                .Where(r => r.HotelId == booking.HotelId)
                .GroupBy(r => r.RoomType.Name)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault().Price);

            var roomGroups = booking.BookingDateRanges
                .GroupBy(br => br.Room.RoomType.Name)
                .Select(group => new
                {
                    RoomType = group.Key,
                    Quantity = group.Count(),
                    Price = roomPrices.ContainsKey(group.Key) ? roomPrices[group.Key] : 0m
                })
                .ToList();

            foreach (var roomGroup in roomGroups)
            {
                var totalRoomPrice = roomGroup.Price * roomGroup.Quantity * (bookingDetailsViewModel.CheckOutDate - bookingDetailsViewModel.CheckInDate).Days;

                bookingDetailsViewModel.RoomDetails.Add(new RoomDetailViewModel
                {
                    RoomType = roomGroup.RoomType,
                    Quantity = roomGroup.Quantity,
                    RoomPrice = roomGroup.Price,
                    TotalPrice = totalRoomPrice
                });
            }

            return View(bookingDetailsViewModel);
        }



        [HttpPost]
        public IActionResult UpdateBookingStatus(int bookingId, int statusId)
        {
            var booking = _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking != null)
            {
                // Get the selected status
                var selectedStatus = _context.Statuses.FirstOrDefault(s => s.Id == statusId);

                if (selectedStatus != null)
                {
                    booking.StatusId = selectedStatus.Id;
                    _context.SaveChanges();
                }
            }

            // Get the referer (previous page URL)
            var refererUrl = Request.Headers["Referer"].ToString();

            // If the referer is not null or empty, redirect to it
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            // Redirect to the BookingDetails page with the bookingId as a query parameter
            return RedirectToAction("BookingDetails", "Booking", new { area = "Admin", id = bookingId });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var booking = _context.Bookings
                .Include(b => b.BookingDateRanges) // Include related data to ensure cascade delete works properly
                .FirstOrDefault(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Remove associated BookingDateRanges first (if needed)
            _context.BookingDateRanges.RemoveRange(booking.BookingDateRanges);

            // Remove the booking itself
            _context.Bookings.Remove(booking);
            _context.SaveChanges();

            return RedirectToAction("ManageBookings");
        }
    }
}
