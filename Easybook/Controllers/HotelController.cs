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
    public class HotelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public HotelController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(SearchViewModel model)
        {
            DateTime today = DateTime.Today;

            DateTime? checkInDate = model.CheckInDate;
            DateTime? checkOutDate = model.CheckOutDate;
            int? adults = model.Adults;
            int? kids = model.Kids;
            string searchQuery = model.SearchQuery;

            // ✅ Validate CheckInDate and CheckOutDate
            if (checkInDate.HasValue && checkInDate.Value < today)
            {
                ModelState.AddModelError("CheckInDate", "Дата на настаняване не може да бъде в миналото.");
            }

            if (checkInDate.HasValue && checkOutDate.HasValue && checkOutDate.Value <= checkInDate.Value)
            {
                ModelState.AddModelError("CheckOutDate", "Дата на напускане не може да бъде преди датата на настаняване.");
            }

            // ✅ Handle validation errors
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessages"] = TempData["ErrorMessages"] != null
                        ? $"{TempData["ErrorMessages"]}\n{error.ErrorMessage}"
                        : error.ErrorMessage;
                }

                return RedirectToAction("Index", "Home");
            }

            var hotelsQuery = _context.Hotels
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.RoomType)
                .Include(h => h.Images)
                .AsQueryable();

            // ✅ Filter by SearchQuery
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                hotelsQuery = hotelsQuery.Where(h =>
                    h.Name.Contains(searchQuery) ||
                    h.City.Contains(searchQuery));
            }

            var hotelsData = await hotelsQuery
                .Select(h => new
                {
                    h.HotelId,
                    h.Name,
                    h.Description,
                    Images = h.Images
                        .Where(img => img.IsMain)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault() ?? "/images/hotels/default.jpg", // Fallback image
                    Rooms = h.Rooms.ToList(),
                })
                .ToListAsync();

            // ✅ Filter hotels by available rooms and guests
            if (checkInDate.HasValue && checkOutDate.HasValue && adults.HasValue && kids.HasValue)
            {
                int totalGuests = adults.Value + kids.Value;

                hotelsData = hotelsData.Where(hotel =>
                    IsCombinationPossible(hotel.Rooms, totalGuests, checkInDate.Value, checkOutDate.Value)
                ).ToList();
            }

            // ✅ Map data to ViewModel
            var hotels = hotelsData.Select(h => new HotelViewModel
            {
                HotelId = h.HotelId,
                Name = h.Name,
                Description = h.Description,
                ImageUrl = h.Images,
                PricePerNight = h.Rooms.Any() ? h.Rooms.Min(r => r.Price) : 0,
                RoomTypes = h.Rooms.Select(r => r.RoomType.Name).Distinct().ToList()
            }).ToList();

            var hotelsDatesViewModel = new HotelsDatesViewModel
            {
                Hotels = hotels,
                SearchParams = model
            };

            return View(hotelsDatesViewModel);
        }


        public async Task<IActionResult> Details(int? id, SearchViewModel model)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the current logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Handle the case where the user is not logged in
                return Unauthorized();
            }

            // Extract values from model
            DateTime? checkInDate = model.CheckInDate;
            DateTime? checkOutDate = model.CheckOutDate;
            int? adults = model.Adults;
            int? kids = model.Kids;

            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.RoomType)
                .Include(h => h.Images)
                .Include(h => h.Reviews)  // Include reviews here
                    .ThenInclude(r => r.User) // Ensure the User is loaded with the Review
                .FirstOrDefaultAsync(m => m.HotelId == id);

            if (hotel == null)
            {
                return NotFound();
            }

            var hotelViewModel = new HotelViewModel
            {
                HotelId = hotel.HotelId,
                UserId = userId,
                Name = hotel.Name,
                Description = hotel.Description,
                // Sort images by IsMain field (true first)
                Images = hotel.Images
                           .OrderByDescending(img => img.IsMain)  // Sort images, putting IsMain=true first
                           .Select(img => img.ImageUrl)           // Select the ImageUrl for the view model
                           .ToList(),
                PricePerNight = hotel.Rooms.Any() ? hotel.Rooms.Min(r => r.Price) : 0m,
                RoomTypes = hotel.Rooms.Select(r => r.RoomType.Name).Distinct().ToList(),
                Reviews = hotel.Reviews.ToList()
            };

            if (checkInDate.HasValue && checkOutDate.HasValue && adults.HasValue && kids.HasValue)
            {
                int totalGuests = adults.Value + kids.Value;

                // Get both combinations
                var exactFit = GetRoomCombinations(hotel.Rooms.ToList(), totalGuests, checkInDate.Value, checkOutDate.Value);

                hotelViewModel.ExactFitCombination = string.Join(";", exactFit.Select(c => $"{c.RoomTypeName}:{c.RoomCount}"));
            }

            return View(hotelViewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(Review review, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Add the review to the database
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Redirect back to the hotel details page (use the returnUrl if provided)
                return Redirect(returnUrl ?? Url.Action("Details", "Hotel", new { id = review.HotelId }));
            }

            // If the model state is invalid, return the form again
            return View();
        }




        [HttpPost]
        public async Task<IActionResult> IsCustomCombinationPossible(int HotelId, Dictionary<string, int> roomTypeCounts, SearchViewModel searchViewModel)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(m => m.HotelId == HotelId);

            if (hotel == null)
            {
                return NotFound();
            }

            // Get available rooms for the specified dates
            var availableRooms = GetAvailableRooms(hotel.Rooms.ToList(), searchViewModel.CheckInDate, searchViewModel.CheckOutDate);

            // Create a dictionary to track the available rooms by type
            var availableRoomCounts = availableRooms
                .GroupBy(r => r.RoomType.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            // Now we check if the available rooms meet the requirements in roomTypeCounts
            foreach (var roomTypeCount in roomTypeCounts)
            {
                string roomTypeName = roomTypeCount.Key;
                int requiredCount = roomTypeCount.Value;

                // Check if there are enough available rooms of the required type
                if (!availableRoomCounts.ContainsKey(roomTypeName) || availableRoomCounts[roomTypeName] < requiredCount)
                {
                    TempData["ErrorMessage"] = "Избраната комбинация от стаи не е налична. Моля, изберете друга.";

                    return RedirectToAction("Details", new
                    {
                        id = HotelId,
                        CheckInDate = searchViewModel.CheckInDate,
                        CheckOutDate = searchViewModel.CheckOutDate,
                        Adults = searchViewModel.Adults,
                        Kids = searchViewModel.Kids
                    });
                }
            }

            // If all checks passed, redirect to the booking page
            return RedirectToAction("BookingGet","Booking", new 
            { 
                HotelId = HotelId,
                SelectedCombination = string.Join(";", roomTypeCounts.Select(r => $"{r.Key}:{r.Value}")),
                CheckInDate = searchViewModel.CheckInDate,
                CheckOutDate = searchViewModel.CheckOutDate 
            });
        }

        //Checks if there is a possible combination for the user's input (used in index method)
        private bool IsCombinationPossible(List<Room> rooms, int totalGuests, DateTime checkInDate, DateTime checkOutDate)
        {
            // Get available rooms for the selected dates
            var availableRooms = rooms
                .Where(r => IsRoomAvailable(r, checkInDate, checkOutDate))
                .OrderByDescending(r => r.Capacity) // Prioritize larger rooms first
                .ToList();

            // ✅ If any room is available, allow the booking (even if it's larger than the guest count)
            return availableRooms.Any();
        }

        private List<(string RoomTypeName, int RoomCount)>
        GetRoomCombinations(List<Room> rooms, int totalGuests, DateTime checkInDate, DateTime checkOutDate)
        {
            // Step 1: Get available rooms
            var availableRooms = GetAvailableRooms(rooms, checkInDate, checkOutDate);

            // Step 2: Calculate the exact fit combination
            var exactFitCombination = FindExactCombination(availableRooms, totalGuests);

            return exactFitCombination;
        }

        private List<Room> GetAvailableRooms(List<Room> rooms, DateTime checkInDate, DateTime checkOutDate)
        {
            return rooms
                .Where(r => IsRoomAvailable(r, checkInDate, checkOutDate))
                .OrderByDescending(r => r.Capacity) // Prioritize larger rooms
                .ThenBy(r => r.Price)               // Then prioritize cheaper rooms
                .ToList();
        }

        private List<(string RoomTypeName, int RoomCount)> FindExactCombination(List<Room> rooms, int totalGuests)
        {
            var bestCombination = new List<(string RoomTypeName, int RoomCount)>();
            int remainingGuests = totalGuests;

            foreach (var room in rooms)
            {
                if (remainingGuests <= 0)
                    break;

                int roomCount = remainingGuests / room.Capacity;
                if (roomCount > 0)
                {
                    bestCombination.Add((room.RoomType.Name, roomCount));
                    remainingGuests -= roomCount * room.Capacity;
                }
            }

            return remainingGuests == 0 ? bestCombination : new List<(string RoomTypeName, int RoomCount)>();
        }

        private bool IsRoomAvailable(Room room, DateTime checkInDate, DateTime checkOutDate)
        {
            // Check if the room is available for the given date range
            var overlappingBookings = _context.BookingDateRanges
                .Any(bdr => bdr.RoomId == room.RoomId &&
                            (bdr.StartDate < checkOutDate && bdr.EndDate > checkInDate));

            return !overlappingBookings;
        }


    }
}
