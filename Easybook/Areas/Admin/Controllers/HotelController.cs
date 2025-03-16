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

                return RedirectToAction("ManageHotels");
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
                MainImageIndex = hotel.Images?.FirstOrDefault(i => i.IsMain)?.ImageId.ToString(),
                AllFacilities = _context.Facilities.ToList(),
                // Selected facilities
                SelectedFacilityIds = string.Join(',', hotel.HotelFacilities.Select(hf => hf.FacilityId.ToString()))
            };

            return View(viewModel);
        }


        // POST: Admin/Hotel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HotelEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllFacilities = _context.Facilities.ToList();
                return View(model);
            }

            var hotel = await _context.Hotels
                .Include(h => h.Images)
                .Include(h => h.HotelFacilities)
                .FirstOrDefaultAsync(h => h.HotelId == model.HotelId);

            if (hotel == null)
            {
                return NotFound();
            }

            // Update hotel details
            hotel.Name = model.Name;
            hotel.Address = model.Address;
            hotel.City = model.City;
            hotel.Country = model.Country;
            hotel.Description = model.Description;

            // Update room info (Single, Double, Family)
            // Assuming you have a method to update room counts and prices
            UpdateRoomInfo(hotel, model);

            // Update facilities (many-to-many relationship)
            if (model.SelectedFacilityIds != null)
            {
                // Convert the comma-separated string of facility IDs into a List of Integers
                var selectedFacilityIds = model.SelectedFacilityIds
                    .Split(',')
                    .Select(id => int.Parse(id))  // Convert each ID to an integer
                    .ToList();

                // Get the existing facilities linked to the hotel
                var existingFacilities = _context.HotelFacilities
                    .Where(hf => hf.HotelId == hotel.HotelId)
                    .ToList();

                // Identify the facility IDs that are currently linked to the hotel
                var currentFacilityIds = existingFacilities.Select(hf => hf.FacilityId).ToList();

                // 1. Remove facilities that are no longer selected (present in current but not in selected list)
                var facilitiesToRemove = existingFacilities
                    .Where(hf => !selectedFacilityIds.Contains(hf.FacilityId))
                    .ToList();

                _context.HotelFacilities.RemoveRange(facilitiesToRemove);

                // 2. Add new facilities that are selected but not yet linked to the hotel (present in selected but not in current list)
                var facilitiesToAdd = selectedFacilityIds
                    .Where(id => !currentFacilityIds.Contains(id))
                    .Select(id => new HotelFacilities
                    {
                        HotelId = hotel.HotelId,
                        FacilityId = id
                    })
                    .ToList();

                _context.HotelFacilities.AddRange(facilitiesToAdd);

                // Save the changes to the database
                _context.SaveChanges();
            }




            // Remove deleted images
            var imagesForDeletion = model.ImagesForDeletion?.Split(',').Select(int.Parse).ToList();
            if (imagesForDeletion != null)
            {
                var imagesToDelete = hotel.Images.Where(i => imagesForDeletion.Contains(i.ImageId)).ToList();
                _context.Images.RemoveRange(imagesToDelete);
            }

            // Add new images
            if (model.NewImages != null && model.NewImages.Any())
            {

                for (int i = 0; i < model.NewImages.Count; i++)
                {
                    var imageFile = model.NewImages[i];
                    var fileName = Path.Combine(Guid.NewGuid() + "_" + imageFile.FileName);
                    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels", fileName);

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    var image = new Image
                    {
                        HotelId = hotel.HotelId,
                        ImageUrl = "/images/hotels/" + fileName, // Store relative path
                        IsMain = false
                    };

                    _context.Images.Add(image);
                }
            }

            // Save changes to the database (new IDs assigned here)
            await _context.SaveChangesAsync();

            // Handle the main image after new images are saved
            if (!string.IsNullOrEmpty(model.MainImageIndex))
            {
                if (model.MainImageIndex.StartsWith("new-"))
                {
                    // Match new image by index
                    int newImageIndex = int.Parse(model.MainImageIndex.Replace("new-", ""));

                    // Get the list of most recently added images
                    var newImages = hotel.Images
                        .OrderByDescending(i => i.ImageId)
                        .Take(model.NewImages.Count)
                        .ToList();

                    // Map by index (based on order they were added)
                    if (newImageIndex < newImages.Count)
                    {
                        var newMainImage = newImages[newImageIndex];

                        if (newMainImage != null)
                        {
                            // Unset other main images
                            hotel.Images.ToList().ForEach(i => i.IsMain = false);

                            // Set the newly added image as main
                            newMainImage.IsMain = true;
                        }
                    }
                }
                else
                {
                    // Handle existing image case
                    int mainImageId = int.Parse(model.MainImageIndex);
                    var mainImage = hotel.Images.FirstOrDefault(i => i.ImageId == mainImageId);

                    if (mainImage != null)
                    {
                        hotel.Images.ToList().ForEach(i => i.IsMain = false);
                        mainImage.IsMain = true;
                    }
                }
            }

            // Save changes again to update IsMain
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageHotels"); // Redirect to the list of hotels or any other page
        }

        public async Task<IActionResult> Delete(int id)
        {
            var hotel = await _context.Hotels
                                    .Include(h => h.Rooms)
                                    .Include(h => h.HotelFacilities)
                                    .Include(h => h.Images)
                                    .Include(h => h.Bookings) // Include related bookings
                                    .FirstOrDefaultAsync(h => h.HotelId == id);

            if (hotel == null)
            {
                return NotFound();
            }

            // Deleting related rooms, bookings, facilities, and images
            // Delete related rooms
            _context.Rooms.RemoveRange(hotel.Rooms);

            // Delete related facilities
            _context.HotelFacilities.RemoveRange(hotel.HotelFacilities);

            // Delete related images from the filesystem
            foreach (var image in hotel.Images)
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/')); // Get full path from the relative URL
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath); // Delete the file from the file system
                }
            }

            // Delete related images
            _context.Images.RemoveRange(hotel.Images);

            // Delete related bookings (if necessary)
            _context.Bookings.RemoveRange(hotel.Bookings);

            // Finally, delete the hotel itself
            _context.Hotels.Remove(hotel);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageHotels)); // Redirect to manage hotels page after deletion
        }

        // Helper method to update room info
        private void UpdateRoomInfo(Hotel hotel, HotelEditViewModel model)
        {
            // Update the count and price for each room type

            var hotelWithRooms = _context.Hotels
                             .Include(h => h.Rooms) // Include rooms
                             .ThenInclude(r => r.RoomType) // Include RoomType for rooms
                             .FirstOrDefault(h => h.HotelId == hotel.HotelId);

            // For single rooms
            var singleRooms = hotel.Rooms.Where(r => r.RoomType.Name == "Единична").ToList();
            if (singleRooms != null && singleRooms.Any())
            {
                // Update price for all existing single rooms
                foreach (var room in singleRooms)
                {
                    room.Price = model.SingleRoomPrice; // Update the price for existing rooms
                }

                // Add new rooms only if the count is greater than the current count
                if (model.SingleRoomCount > singleRooms.Count)
                {
                    var roomTypeId = singleRooms.First().RoomTypeId;  // Get RoomTypeId from the first room in the list

                    for (int i = singleRooms.Count; i < model.SingleRoomCount; i++)
                    {
                        _context.Rooms.Add(new Room
                        {
                            HotelId = hotel.HotelId,
                            RoomTypeId = roomTypeId,  // Use the correct RoomTypeId
                            Capacity = 1, // Assuming single rooms have capacity of 1
                            Price = model.SingleRoomPrice
                        });
                    }
                }
            }

            // For double rooms
            var doubleRooms = hotel.Rooms.Where(r => r.RoomType.Name == "Двойна").ToList();
            if (doubleRooms.Any())
            {
                // Update price for all existing double rooms
                foreach (var room in doubleRooms)
                {
                    room.Price = model.DoubleRoomPrice; // Update the price for existing rooms
                }

                // Add new rooms only if the count is greater than the current count
                if (model.DoubleRoomCount > doubleRooms.Count)
                {
                    var roomTypeId = doubleRooms.First().RoomTypeId;  // Get RoomTypeId from the first room in the list

                    for (int i = doubleRooms.Count; i < model.DoubleRoomCount; i++)
                    {
                        _context.Rooms.Add(new Room
                        {
                            HotelId = hotel.HotelId,
                            RoomTypeId = roomTypeId,  // Use the correct RoomTypeId
                            Capacity = 2, // Assuming double rooms have capacity of 2
                            Price = model.DoubleRoomPrice
                        });
                    }
                }
            }

            // For family rooms
            var familyRooms = hotel.Rooms.Where(r => r.RoomType.Name == "Семейна").ToList();
            if (familyRooms.Any())
            {
                // Update price for all existing family rooms
                foreach (var room in familyRooms)
                {
                    room.Price = model.FamilyRoomPrice; // Update the price for existing rooms
                }

                // Add new rooms only if the count is greater than the current count
                if (model.FamilyRoomCount > familyRooms.Count)
                {
                    var roomTypeId = familyRooms.First().RoomTypeId;  // Get RoomTypeId from the first room in the list

                    for (int i = familyRooms.Count; i < model.FamilyRoomCount; i++)
                    {
                        _context.Rooms.Add(new Room
                        {
                            HotelId = hotel.HotelId,
                            RoomTypeId = roomTypeId,  // Use the correct RoomTypeId
                            Capacity = 4, // Assuming family rooms have capacity of 4
                            Price = model.FamilyRoomPrice
                        });
                    }
                }
            }

            // Save the changes to the database
            _context.SaveChanges();
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
