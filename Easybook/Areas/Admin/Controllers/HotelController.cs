using Easybook.Areas.Admin.Models.ViewModels;
using Easybook.Data;
using Easybook.Models;
using Easybook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Easybook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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

        public IActionResult ManageHotels()
        {
            var hotels = _context.Hotels.ToList(); // Get all hotels from the database
            return View(hotels); // Pass the hotels to the view
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

        public async Task<IActionResult> Edit(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.HotelFacilities)
                .ThenInclude(hf => hf.Facility)
                .Include(h => h.Images) // Include images
                .Include(h => h.Rooms)  // Include rooms to fetch room prices
                    .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(h => h.HotelId == id);

            if (hotel == null)
            {
                return NotFound();
            }

            // Create the ViewModel for editing
            var viewModel = new HotelEditViewModel
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City,
                Country = hotel.Country,
                Description = hotel.Description,
                // Populate room data from the existing rooms
                SingleRoomCount = hotel.Rooms.Count(r => r.RoomType.Name == "Единична"),
                SingleRoomPrice = hotel.Rooms.FirstOrDefault(r => r.RoomType.Name == "Единична")?.Price ?? 0,
                DoubleRoomCount = hotel.Rooms.Count(r => r.RoomType.Name == "Двойна"),
                DoubleRoomPrice = hotel.Rooms.FirstOrDefault(r => r.RoomType.Name == "Двойна")?.Price ?? 0,
                FamilyRoomCount = hotel.Rooms.Count(r => r.RoomType.Name == "Семейна"),
                FamilyRoomPrice = hotel.Rooms.FirstOrDefault(r => r.RoomType.Name == "Семейна")?.Price ?? 0,
                // Set Images for editing (already associated images)
                Images = hotel.Images,
                // Select the main image based on the saved flag
                MainImageIndex = hotel.Images?.FirstOrDefault(i => i.IsMain)?.ImageId,
                AllFacilities = _context.Facilities.ToList(),
                // Selected facilities
                SelectedFacilityIds = hotel.HotelFacilities.Select(hf => hf.FacilityId.ToString()).ToArray()
            };

            return View(viewModel);
        }





        // POST method to handle form submission for editing
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize]
        //public async Task<IActionResult> Edit(int id, HotelEditViewModel viewModel)
        //{
        //    if (id != viewModel.HotelId)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        // Get the existing hotel
        //        var hotel = await _context.Hotels.Include(h => h.HotelFacilities).FirstOrDefaultAsync(h => h.HotelId == id);

        //        if (hotel == null)
        //        {
        //            return NotFound();
        //        }

        //        // Update hotel details
        //        hotel.Name = viewModel.Name;
        //        hotel.Address = viewModel.Address;
        //        hotel.City = viewModel.City;
        //        hotel.Country = viewModel.Country;
        //        hotel.Description = viewModel.Description;
        //        hotel.SingleRoomCount = viewModel.SingleRoomCount;
        //        hotel.SingleRoomPrice = viewModel.SingleRoomPrice;
        //        hotel.DoubleRoomCount = viewModel.DoubleRoomCount;
        //        hotel.DoubleRoomPrice = viewModel.DoubleRoomPrice;
        //        hotel.FamilyRoomCount = viewModel.FamilyRoomCount;
        //        hotel.FamilyRoomPrice = viewModel.FamilyRoomPrice;

        //        // Remove old facilities
        //        var existingFacilities = _context.HotelFacilities.Where(hf => hf.HotelId == hotel.HotelId).ToList();
        //        _context.HotelFacilities.RemoveRange(existingFacilities);

        //        // Add new facilities
        //        if (viewModel.SelectedFacilityIds != null)
        //        {
        //            var selectedFacilityIds = viewModel.SelectedFacilityIds
        //                .Select(int.Parse)
        //                .ToList();

        //            foreach (var facilityId in selectedFacilityIds)
        //            {
        //                _context.HotelFacilities.Add(new HotelFacilities
        //                {
        //                    HotelId = hotel.HotelId,
        //                    FacilityId = facilityId
        //                });
        //            }
        //        }

        //        // Handle new images (uploaded by user)
        //        if (viewModel.NewImages != null && viewModel.NewImages.Any())
        //        {
        //            for (int i = 0; i < viewModel.NewImages.Count; i++)
        //            {
        //                var imageFile = viewModel.NewImages[i];
        //                var fileName = Path.Combine(Guid.NewGuid() + "_" + imageFile.FileName);
        //                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels", fileName);

        //                // Save the file to the server
        //                using (var stream = new FileStream(uploadPath, FileMode.Create))
        //                {
        //                    await imageFile.CopyToAsync(stream);
        //                }

        //                var isMain = i == viewModel.MainImageIndex; // Check if this is the main image

        //                var image = new Image
        //                {
        //                    HotelId = hotel.HotelId,
        //                    ImageUrl = "/images/hotels/" + fileName,
        //                    IsMain = isMain
        //                };

        //                _context.Images.Add(image);
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        return RedirectToAction(nameof(ManageHotels)); // Redirect to ManageHotels or another suitable action
        //    }

        //    // If validation fails, reload facilities for the view
        //    viewModel.AllFacilities = _context.Facilities.ToList();
        //    return View(viewModel);
        //}




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

        private async Task UpdateRooms(int hotelId, int count, string roomTypeName, decimal roomPrice)
        {
            var roomType = await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.Name == roomTypeName);

            if (roomType != null)
            {
                var existingRooms = _context.Rooms.Where(r => r.HotelId == hotelId && r.RoomTypeId == roomType.RoomTypeId).ToList();

                // Remove existing rooms if count is less than the requested
                if (existingRooms.Count > count)
                {
                    _context.Rooms.RemoveRange(existingRooms.Skip(count));
                }

                // Add new rooms if needed
                for (int i = existingRooms.Count; i < count; i++)
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

    }
}
