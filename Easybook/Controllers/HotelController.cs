using Easybook.Data;
using Easybook.Models;
using Easybook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Easybook.Controllers
{
    public class HotelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HotelController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(DateTime? checkInDate, DateTime? checkOutDate, int? adults, int? kids)
        {
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

            // Fetch the hotel along with related rooms and images
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.RoomType) // Including room type details
                .Include(h => h.Images)
                .FirstOrDefaultAsync(m => m.HotelId == id);

            if (hotel == null)
            {
                return NotFound();
            }

            // Create a view model to pass the required data to the view
            var hotelViewModel = new HotelViewModel
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Description = hotel.Description,
                Images = hotel.Images.Select(img => img.ImageUrl).ToList(),
                PricePerNight = hotel.Rooms.Any() ? hotel.Rooms.Min(r => r.Price) : 0m, // Use null if no rooms are available
                RoomTypes = hotel.Rooms
                    .Select(r => r.RoomType.Name.ToString())
                    .Distinct()
                    .ToList()
            };

            // Handle case when no room types are available
            if (hotelViewModel.RoomTypes == null || !hotelViewModel.RoomTypes.Any())
            {
                hotelViewModel.RoomTypes = new List<string> { "Няма налични типове стаи." };
            }

            // Return best 2-3 combinations if available
            if (checkInDate.HasValue && checkOutDate.HasValue && adults.HasValue && kids.HasValue)
            {
                int totalGuests = adults.Value + kids.Value;

                var bestCombinations = GetBestRoomCombinations(hotel.Rooms.ToList(), totalGuests, checkInDate.Value, checkOutDate.Value);

                hotelViewModel.RoomTypes = bestCombinations;
            }

            return View(hotelViewModel);
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
        private bool IsRoomAvailable(Room room, DateTime checkInDate, DateTime checkOutDate)
        {
            // Check if room is available for the given date range
            var overlappingBookings = _context.BookingDateRanges
                .Any(bdr => bdr.RoomId == room.RoomId &&
                            (bdr.StartDate < checkOutDate && bdr.EndDate > checkInDate));

            return !overlappingBookings; // Return true if the room is available
        }
        private List<string> GetBestRoomCombinations(List<Room> rooms, int totalGuests, DateTime checkInDate, DateTime checkOutDate)
        {
            // Filter rooms based on availability and capacity
            var availableRooms = rooms
                .Where(r => IsRoomAvailable(r, checkInDate, checkOutDate)) // Check room availability for the dates
                .OrderByDescending(r => r.Capacity) // Sort by capacity
                .ToList();

            int remainingGuests = totalGuests;
            List<string> bestCombinations = new List<string>();

            // Try to find the best combinations
            foreach (var room in availableRooms)
            {
                if (remainingGuests <= 0)
                    break;

                if (room.Capacity <= remainingGuests)
                {
                    remainingGuests -= room.Capacity;
                    bestCombinations.Add($"{room.RoomType.Name} ({room.Capacity} guests)");
                }
            }

            // Return best combinations or a default message
            if (bestCombinations.Count == 0)
            {
                bestCombinations.Add("Няма налични комбинации за тези дати.");
            }

            return bestCombinations.Take(3).ToList(); // Return top 2-3 combinations
        }

    }
}
