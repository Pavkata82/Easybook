using Easybook.Areas.Admin.Models.ViewModels;
using Easybook.Constants;
using Easybook.Data;
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

            // Prepare the ViewModel
            var bookingDetailsViewModel = new BookingDetailsViewModel
            {
                BookingId = booking.BookingId,
                CustomerName = booking.User?.UserName,
                Status = booking.Status?.Name,
                HotelName = booking.Hotel?.Name,
                TotalPrice = booking.TotalPrice,
                SpecialRequests = booking.SpecialRequests,
                CheckInDate = booking.BookingDateRanges.Select(bdr => bdr.StartDate).First(),
                CheckOutDate = booking.BookingDateRanges.Select(bdr => bdr.EndDate).First(),
                RoomDetails = new List<RoomDetailViewModel>()
            };

            // Fetch room types and their prices
            var roomPrices = _context.Rooms
                .Where(r => r.HotelId == booking.HotelId)
                .GroupBy(r => r.RoomType.Name)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault().Price);

            // Populate the RoomDetails with RoomType, Quantity, Price, and Total Price for the entire stay
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
                var totalRoomPrice = roomGroup.Price * roomGroup.Quantity * bookingDetailsViewModel.NumberOfDays;

                // Add the RoomDetailViewModel to RoomDetails
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


    }
}
