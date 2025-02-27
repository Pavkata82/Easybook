using Easybook.Data;
using Easybook.Models;
using Easybook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Easybook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> BookingGet(BookingViewModel model)
        {
            if (model.SelectedCombination == null)
            {
                return BadRequest("Комбинацията не е предоставена.");
            }

            if (!model.SelectedCombination.Any())
            {
                return BadRequest("Комбинацията не съдържа валидни стаи.");
            }

            // Get the logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Неуспешно идентифициране на потребителя.");
            }

            model.PhoneNumber = (await _userManager.FindByIdAsync(userId)).PhoneNumber;

            // Initialize the TotalPrice to 0
            model.TotalPrice = 0;

            // Parse the selected combination (e.g., "roomType:count;roomType:count")
            var parts = model.SelectedCombination.Split(';').ToList();

            // Fetch all room types and their prices for the hotel once
            var roomTypesAndPrices = await _context.Rooms
                .Where(r => r.HotelId == model.HotelId)
                .GroupBy(r => r.RoomType.Name)
                .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault().Price);

            model.RoomTypesAndPrices = roomTypesAndPrices;

            foreach (var part in parts)
            {
                var roomParts = part.Split(':');
                if (roomParts.Length == 2)
                {
                    var roomTypeName = roomParts[0];
                    var roomCount = int.Parse(roomParts[1]);

                    // Check if the room type exists for this hotel
                    if (roomTypesAndPrices.TryGetValue(roomTypeName, out decimal roomPrice))
                    {
                        // Calculate the price for this room type * count of rooms * number of nights
                        decimal totalRoomPrice = roomPrice * roomCount * (model.CheckOutDate - model.CheckInDate).Days;

                        // Add the room total price to the overall total price
                        model.TotalPrice += totalRoomPrice;
                    }
                    else
                    {
                        return BadRequest($"Не намерихме стаи от тип {roomTypeName} за този хотел.");
                    }
                }
                else
                {
                    return BadRequest("Невалиден формат на комбинацията от стаи.");
                }
            }

            // Prevent initial validation error
            ModelState.Clear();

            // Return the view with the calculated total price
            return View("Booking", model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BookingPost(BookingViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Неуспешно идентифициране на потребителя.");
            }

            if (!ModelState.IsValid)
            {
                model.PhoneNumber = (await _userManager.FindByIdAsync(userId)).PhoneNumber;

                // Initialize the TotalPrice to 0
                model.TotalPrice = 0;

                // Parse the selected combination (e.g., "roomType:count;roomType:count")
                var parts = model.SelectedCombination.Split(';').ToList();

                // Fetch all room types and their prices for the hotel once
                var roomTypesAndPrices = await _context.Rooms
                    .Where(r => r.HotelId == model.HotelId)
                    .GroupBy(r => r.RoomType.Name)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault().Price);

                model.RoomTypesAndPrices = roomTypesAndPrices;

                foreach (var part in parts)
                {
                    var roomParts = part.Split(':');
                    if (roomParts.Length == 2)
                    {
                        var roomTypeName = roomParts[0];
                        var roomCount = int.Parse(roomParts[1]);

                        // Check if the room type exists for this hotel
                        if (roomTypesAndPrices.TryGetValue(roomTypeName, out decimal roomPrice))
                        {
                            // Calculate the price for this room type * count of rooms * number of nights
                            decimal totalRoomPrice = roomPrice * roomCount * (model.CheckOutDate - model.CheckInDate).Days;

                            // Add the room total price to the overall total price
                            model.TotalPrice += totalRoomPrice;
                        }
                        else
                        {
                            return BadRequest($"Не намерихме стаи от тип {roomTypeName} за този хотел.");
                        }
                    }
                    else
                    {
                        return BadRequest("Невалиден формат на комбинацията от стаи.");
                    }
                }

                return View("Booking", model); // Return the view with validation errors
            }

            if (string.IsNullOrEmpty(model.SpecialRequests))
            {
                model.SpecialRequests = ""; // Ensure it is not null
            }

            // Get the user's phone number if not already set
            var user = await _userManager.FindByIdAsync(userId);

            if (string.IsNullOrEmpty(model.PhoneNumber) && string.IsNullOrEmpty(user.PhoneNumber))
            {
                // If the user doesn't have a phone number, make it required
                ModelState.AddModelError("PhoneNumber", "Моля, попълнете телефонен номер.");
                return View("Booking", model); // Return the view with the error
            }

            if (string.IsNullOrEmpty(user.PhoneNumber) && !string.IsNullOrEmpty(model.PhoneNumber))
            {
                // If the user doesn't have a phone number but provided one, update it
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Calculate total price and check availability
                decimal totalPrice = 0m;
                var bookingDateRanges = new List<BookingDateRange>();

                foreach (var roomCombination in model.SelectedCombination.Split(';'))
                {
                    var roomParts = roomCombination.Split(':');
                    if (roomParts.Length == 2)
                    {
                        var RoomTypeName = roomParts[0];
                        var RoomCount = int.Parse(roomParts[1]);

                        var rooms = await _context.Rooms
                            .Where(r => r.HotelId == model.HotelId &&
                                        r.RoomType.Name == RoomTypeName &&
                                        !r.BookingDateRanges.Any(bdr =>
                                            bdr.StartDate < model.CheckOutDate && bdr.EndDate > model.CheckInDate))
                            .Take(RoomCount)
                            .ToListAsync();

                        if (rooms.Count < RoomCount)
                        {
                            return BadRequest($"Недостатъчно свободни стаи за {RoomTypeName}.");
                        }

                        totalPrice += rooms.Sum(r => r.Price) * (model.CheckOutDate - model.CheckInDate).Days;

                        // Add BookingDateRanges for each room
                        foreach (var room in rooms)
                        {
                            bookingDateRanges.Add(new BookingDateRange
                            {
                                RoomId = room.RoomId,
                                StartDate = model.CheckInDate,
                                EndDate = model.CheckOutDate
                            });
                        }
                    }
                    else
                    {
                        return BadRequest("Невалиден формат на комбинацията от стаи.");
                    }
                }


                // Create and save the booking
                var booking = new Booking
                {
                    UserId = userId,
                    HotelId = model.HotelId,
                    TotalPrice = totalPrice,
                    StatusId = 2, // Pending
                    SpecialRequests = model.SpecialRequests
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Assign BookingId to BookingDateRanges and save
                foreach (var dateRange in bookingDateRanges)
                {
                    dateRange.BookingId = booking.BookingId;
                }

                _context.BookingDateRanges.AddRange(bookingDateRanges);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("Грешка при записване на резервацията.");
            }
        }

        [Authorize]
        public async Task<IActionResult> BookingConfirmation(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Get the logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("Неуспешно идентифициране на потребителя.");
            }

            // Get the user's phone number if not already set
            var user = await _userManager.FindByIdAsync(userId);
            if (user.FirstName != null || user.LastName != null)
            {
                var fullName = $"{user.FirstName?.FirstOrDefault().ToString().ToUpper()}{user.FirstName?.Substring(1) ?? ""} {user.LastName?.FirstOrDefault().ToString().ToUpper()}{user.LastName?.Substring(1) ?? ""}".Trim();

                ViewBag.FullName = fullName;
            }


            var hotel = booking.Hotel; // Retrieve hotel details for confirmation


            // Pass the data to the view
            ViewBag.HotelName = hotel.Name;

            return View();
        }
    }
}
