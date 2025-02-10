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

        public async Task<IActionResult> Index(DateTime? checkInDate, DateTime? checkOutDate, int? adults, int? kids)
        {
            // Get today's date and format it
            DateTime today = DateTime.Today;

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

            return View(hotels);
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
        public async Task<IActionResult> Details(int? id, DateTime? checkInDate, DateTime? checkOutDate, int? adults, int? kids)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                var (exactFit, minimalEmptySpots) = GetRoomCombinations(hotel.Rooms.ToList(), totalGuests, checkInDate.Value, checkOutDate.Value);

                hotelViewModel.ExactFitCombination = exactFit;
                hotelViewModel.MinimalEmptySpotsCombination = minimalEmptySpots;
            }

            return View(hotelViewModel);
        }

        [Authorize]
        public IActionResult Booking(int hotelId, string combination)
        {
            if (string.IsNullOrWhiteSpace(combination))
            {
                return BadRequest("Комбинацията не е предоставена.");
            }

            var parsedCombination = new List<(string RoomTypeName, int RoomCount)>();

            try
            {
                parsedCombination = combination.Split(';')
                    .Select(c =>
                    {
                        var parts = c.Split(':');
                        if (parts.Length != 2 || !int.TryParse(parts[1], out int roomCount))
                        {
                            throw new FormatException("Невалиден формат на комбинацията.");
                        }
                        return (RoomTypeName: parts[0], RoomCount: roomCount);
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                return BadRequest($"Грешка при обработка на комбинацията: {ex.Message}");
            }

            if (!parsedCombination.Any())
            {
                return BadRequest("Комбинацията не съдържа валидни стаи.");
            }

            ViewBag.HotelId = hotelId;
            ViewBag.Combination = parsedCombination;

            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Booking(int hotelId,
        string combination,
        string specialRequests,
        DateTime checkInDate,
        DateTime checkOutDate,
        string phoneNumber)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the logged-in user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized("Неуспешно идентифициране на потребителя.");
                }

                // Get the user's phone number if not already set
                var user = await _userManager.FindByIdAsync(userId);
                if (user.PhoneNumber == null && !string.IsNullOrWhiteSpace(phoneNumber))
                {
                    user.PhoneNumber = phoneNumber;
                    await _userManager.UpdateAsync(user);
                }

                // Parse room combinations
                var parsedCombination = combination.Split(';')
                    .Select(c =>
                    {
                        var parts = c.Split(':');
                        return (RoomTypeName: parts[0], RoomCount: int.Parse(parts[1]));
                    })
                    .ToList();

                // Calculate total price and check availability
                decimal totalPrice = 0m;
                var bookingDateRanges = new List<BookingDateRange>();

                foreach (var (RoomTypeName, RoomCount) in parsedCombination)
                {
                    var rooms = await _context.Rooms
                        .Where(r => r.HotelId == hotelId &&
                                    r.RoomType.Name == RoomTypeName &&
                                    !r.BookingDateRanges.Any(bdr =>
                                        bdr.StartDate < checkOutDate && bdr.EndDate > checkInDate))
                        .Take(RoomCount)
                        .ToListAsync();

                    if (rooms.Count < RoomCount)
                    {
                        return BadRequest($"Недостатъчно свободни стаи за {RoomTypeName}.");
                    }

                    totalPrice += rooms.Sum(r => r.Price) * (checkOutDate - checkInDate).Days;

                    // Add BookingDateRanges for each room
                    foreach (var room in rooms)
                    {
                        bookingDateRanges.Add(new BookingDateRange
                        {
                            RoomId = room.RoomId,
                            StartDate = checkInDate,
                            EndDate = checkOutDate
                        });
                    }
                }

                // Create and save the booking
                var booking = new Booking
                {
                    UserId = userId,
                    HotelId = hotelId,
                    TotalPrice = totalPrice,
                    StatusId = 2, // Pending
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

            var hotel = booking.Hotel; // Retrieve hotel details for confirmation
            var fullName = User.Identity.Name; // Assuming the name is stored here, adjust as needed

            // Pass the data to the view
            ViewBag.HotelId = hotel.HotelId;
            ViewBag.FullName = fullName;

            return View();
        }



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
        private (List<(string RoomTypeName, int RoomCount)> ExactFitCombination,
         List<(string RoomTypeName, int RoomCount)> MinimalEmptySpotsCombination)
        GetRoomCombinations(List<Room> rooms, int totalGuests, DateTime checkInDate, DateTime checkOutDate)
        {
            // Step 1: Get available rooms
            var availableRooms = GetAvailableRooms(rooms, checkInDate, checkOutDate);

            // Step 2: Calculate the exact fit combination
            var exactFitCombination = FindExactCombination(availableRooms, totalGuests);

            // Step 3: Calculate the minimal empty spots combination
            var minimalEmptySpotsCombination = FindBestFitCombination(availableRooms, totalGuests);

            return (exactFitCombination, minimalEmptySpotsCombination);
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

        private List<(string RoomTypeName, int RoomCount)> FindBestFitCombination(List<Room> rooms, int totalGuests)
        {
            var bestCombination = new List<(string RoomTypeName, int RoomCount)>();
            int minimalExtraCapacity = int.MaxValue;

            foreach (var room in rooms)
            {
                var currentCombination = new List<(string RoomTypeName, int RoomCount)>();
                int currentGuests = 0;

                foreach (var selectedRoom in rooms)
                {
                    int roomCount = (totalGuests - currentGuests + selectedRoom.Capacity - 1) / selectedRoom.Capacity; // Round up
                    if (roomCount > 0)
                    {
                        currentCombination.Add((selectedRoom.RoomType.Name, roomCount));
                        currentGuests += roomCount * selectedRoom.Capacity;
                    }
                }

                // Calculate extra capacity
                int extraCapacity = currentGuests - totalGuests;
                if (extraCapacity < minimalExtraCapacity)
                {
                    bestCombination = new List<(string RoomTypeName, int RoomCount)>(currentCombination);
                    minimalExtraCapacity = extraCapacity;
                }
            }

            return bestCombination;
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
