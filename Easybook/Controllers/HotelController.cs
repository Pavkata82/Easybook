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
            // Get today's date and format it
            DateTime today = DateTime.Today;

            // Extract values from model
            DateTime? checkInDate = model.CheckInDate;
            DateTime? checkOutDate = model.CheckOutDate;
            int? adults = model.Adults;
            int? kids = model.Kids;

            // Validate CheckInDate (cannot be before today)
            if (checkInDate.HasValue && checkInDate.Value < today)
            {
                ModelState.AddModelError("checkInDate", "Дата на настаняване не може да бъде в миналото.");
            }

            // Validate CheckOutDate (cannot be before CheckInDate)
            if (checkInDate.HasValue && checkOutDate.HasValue && checkOutDate.Value <= checkInDate.Value)
            {
                ModelState.AddModelError("checkOutDate", "Дата на напускане не може да бъде преди датата на настаняване.");
            }

            // If there are validation errors, return to home view with the error messages
            if (!ModelState.IsValid)
            {
                // Store the validation errors in TempData
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    TempData["ErrorMessages"] = TempData["ErrorMessages"] != null
                        ? $"{TempData["ErrorMessages"]}\n{error.ErrorMessage}"
                        : error.ErrorMessage;
                }

                // Redirect to Home/Index with validation errors stored in TempData
                return RedirectToAction("Index", "Home");
            }

            var hotelsQuery = _context.Hotels
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.RoomType)
                .Include(h => h.Images)
                .AsQueryable();

            // Fetch data from the database first, without applying combination check in the query
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
                    Rooms = h.Rooms.ToList(), // Load rooms with necessary data
                })
                .ToListAsync();

            // Filter hotels based on combination logic and room availability
            if (checkInDate.HasValue && checkOutDate.HasValue && adults.HasValue && kids.HasValue)
            {
                int totalGuests = adults.Value + kids.Value;

                // Apply combination logic and room availability check in-memory
                hotelsData = hotelsData.Where(hotel =>
                    IsCombinationPossible(hotel.Rooms, totalGuests, checkInDate.Value, checkOutDate.Value) // Check combination and availability
                ).ToList();
            }

            // Map to ViewModel
            var hotels = hotelsData.Select(h => new HotelViewModel
            {
                HotelId = h.HotelId,
                Name = h.Name,
                Description = h.Description,
                ImageUrl = h.Images, // Already set to default if null
                PricePerNight = h.Rooms.Any() ? h.Rooms.Min(r => r.Price) : 0, // Minimum price of available rooms
                RoomTypes = h.Rooms.Select(r => r.RoomType.Name).Distinct().ToList()
            }).ToList();

            var hotelsDatesViewModel = new HotelsDatesViewModel
            {
                Hotels = hotels,
                SearchParams = model
            };

            return View(hotelsDatesViewModel);
        }


        [Authorize]
        public IActionResult Create()
        {
            var viewModel = new HotelCreateViewModel
            {
                AllFacilities = _context.Facilities.ToList() // Load available facilities
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(HotelCreateViewModel viewModel)
        {
            if (viewModel.SingleRoomCount == 0 && viewModel.SingleRoomPrice > 0)
            {
                ModelState.AddModelError("SingleRoomPrice", "Не може да приложите цена за единична стая когато броят им е 0.");
            }

            if (viewModel.DoubleRoomCount == 0 && viewModel.DoubleRoomPrice > 0)
            {
                ModelState.AddModelError("DoubleRoomPrice", "Не може да приложите цена за двойна стая когато броят им е 0.");
            }

            if (viewModel.FamilyRoomCount == 0 && viewModel.FamilyRoomPrice > 0)
            {
                ModelState.AddModelError("FamilyRoomPrice", "Не може да приложите цена за семейна стая когато броят им е 0.");
            }


            if (ModelState.IsValid)
            {
                // Save hotel
                var hotel = new Hotel
                {
                    Name = viewModel.Name,
                    Address = viewModel.Address,
                    City = viewModel.City,
                    Country = viewModel.Country,
                    Description = viewModel.Description
                };

                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();


                // Assign facilities
                if (viewModel.SelectedFacilityIds != null)
                {
                    var selectedFacilityIds = viewModel.SelectedFacilityIds
                    .Split(',')
                    .Select(int.Parse)
                    .ToList();

                    foreach (var facilityId in selectedFacilityIds)
                    {
                        _context.HotelFacilities.Add(new HotelFacilities
                        {
                            HotelId = hotel.HotelId,
                            FacilityId = facilityId
                        });
                    }
                }    


                // Bulk-add rooms
                await AddRooms(hotel.HotelId, viewModel.SingleRoomCount, "Единична", viewModel.SingleRoomPrice);
                await AddRooms(hotel.HotelId, viewModel.DoubleRoomCount, "Двойна", viewModel.SingleRoomPrice);
                await AddRooms(hotel.HotelId, viewModel.FamilyRoomCount, "Семейна", viewModel.SingleRoomPrice);

                // Save images
                if (viewModel.Images != null && viewModel.Images.Any())
                {

                    for (int i = 0; i < viewModel.Images.Count; i++)
                    {
                        var imageFile = viewModel.Images[i];
                        var fileName = Path.Combine(Guid.NewGuid() + "_" + imageFile.FileName);
                        var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels", fileName);

                        using (var stream = new FileStream(uploadPath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        var isMain = i == viewModel.MainImageIndex; // Check if this is the main image

                        var image = new Image
                        {
                            HotelId = hotel.HotelId,
                            ImageUrl = "/images/hotels/" + fileName, // Store relative path
                            IsMain = isMain
                        };

                        _context.Images.Add(image);
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // If validation fails, reload facilities for the view
            viewModel.AllFacilities = _context.Facilities.ToList();
            return View(viewModel);
        }
        public async Task<IActionResult> Details(int? id, SearchViewModel model)
        {
            if (id == null)
            {
                return NotFound();
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
                .FirstOrDefaultAsync(m => m.HotelId == id);

            if (hotel == null)
            {
                return NotFound();
            }

            var hotelViewModel = new HotelViewModel
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Description = hotel.Description,
                Images = hotel.Images.Select(img => img.ImageUrl).ToList(),
                PricePerNight = hotel.Rooms.Any() ? hotel.Rooms.Min(r => r.Price) : 0m,
                RoomTypes = hotel.Rooms.Select(r => r.RoomType.Name).Distinct().ToList(),
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
            return RedirectToAction("BookingGet", new 
            { 
                HotelId = HotelId,
                SelectedCombination = string.Join(";", roomTypeCounts.Select(r => $"{r.Key}:{r.Value}")),
                CheckInDate = searchViewModel.CheckInDate,
                CheckOutDate = searchViewModel.CheckOutDate 
            });
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

            ModelState.Clear();

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


        //Checks if there is a possible combination for the user's input (used in index method)
        private bool IsCombinationPossible(List<Room> rooms, int totalGuests, DateTime checkInDate, DateTime checkOutDate) 
        {
            // Simplified room combination check logic
            var availableRooms = rooms
                .Where(r => IsRoomAvailable(r, checkInDate, checkOutDate)) // Check room availability for the dates
                .OrderByDescending(r => r.Capacity) // Sort by capacity
                .ToList();

            int remainingGuests = totalGuests;

            foreach (var room in availableRooms)
            {
                if (remainingGuests <= 0)
                    break;

                if (room.Capacity <= remainingGuests)
                {
                    remainingGuests -= room.Capacity;
                }
            }

            return remainingGuests <= 0; // If no remaining guests, the combination is possible
        }
        private async Task AddRooms(int hotelId, int count, string roomTypeName, decimal roomPrice)
        {
            var roomType = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.Name == roomTypeName);

            if (roomType != null)
            {
                for (int i = 0; i < count; i++)
                {
                    _context.Rooms.Add(new Room
                    {
                        HotelId = hotelId,
                        RoomTypeId = roomType.RoomTypeId,
                        Capacity = roomTypeName == "Единична" ? 1 : roomTypeName == "Двойна" ? 2 : 4,
                        Price = roomPrice
                    });
                }
            }
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
