using Easybook.Models;
using System.Collections.Generic;

namespace Easybook.Areas.Admin.Models.ViewModels
{
    public class HotelEditViewModel
    {
        // Hotel Info
        public int HotelId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }

        // Room Info
        public int SingleRoomCount { get; set; }
        public decimal SingleRoomPrice { get; set; }

        public int DoubleRoomCount { get; set; }
        public decimal DoubleRoomPrice { get; set; }

        public int FamilyRoomCount { get; set; }
        public decimal FamilyRoomPrice { get; set; }

        // Facilities
        public List<Facility> AllFacilities { get; set; } = new List<Facility>();
        public string[] SelectedFacilityIds { get; set; }  // List of selected facility IDs

        // Images
        public ICollection<Image> Images { get; set; } = new List<Image>();  // List of images associated with the hotel
        public int? MainImageIndex { get; set; }  // Index of the main image (if any)

        // For new image uploads (we'll handle the uploaded files in the controller)
        public List<IFormFile> NewImages { get; set; } = new List<IFormFile>();
    }
}
