using System.ComponentModel.DataAnnotations;

namespace Easybook.Models.ViewModels
{
    public class HotelCreateViewModel
    {

        [Required(ErrorMessage = "Моля въведете име на хотела.")]
        [StringLength(100, ErrorMessage = "Името на хотела не може да бъде по-дълго от 100 символа.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Моля въведете адрес на хотела.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Моля въведете град.")]
        [StringLength(50, ErrorMessage = "Името на града не може да бъде по-дълго от 50 символа.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Моля въведете държава.")]
        [StringLength(50, ErrorMessage = "Името на държавата не може да бъде по-дълго от 50 символа.")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Моля въведете описание.")]
        [StringLength(500, ErrorMessage = "Описанието не може да бъде по-дълго от 500 символа.")]
        public string Description { get; set; }

        // Facilities
        public string? SelectedFacilityIds { get; set; }
        public List<Facility> AllFacilities { get; set; } = new List<Facility>();

        // Rooms count
        [Range(0, int.MaxValue, ErrorMessage = "Броят на единичните стаи трябва да бъде положително число.")]
        public int SingleRoomCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Броят на двойните стаи трябва да бъде положително число.")]
        public int DoubleRoomCount { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Броят на семейните стаи трябва да бъде положително число.")]
        public int FamilyRoomCount { get; set; }

        // Room prices
        [Required(ErrorMessage = "Моля въведете цена за единична стая.")]
        [Range(0, double.MaxValue, ErrorMessage = "Цената трябва да бъде положително число.")]
        public decimal SingleRoomPrice { get; set; }

        [Required(ErrorMessage = "Моля въведете цена за двойна стая.")]
        [Range(0, double.MaxValue, ErrorMessage = "Цената трябва да бъде положително число.")]
        public decimal DoubleRoomPrice { get; set; }

        [Required(ErrorMessage = "Моля въведете цена за семейна стая.")]
        [Range(0, double.MaxValue, ErrorMessage = "Цената трябва да бъде положително число.")]
        public decimal FamilyRoomPrice { get; set; }

        // Images
        [Required(ErrorMessage = "Моля изберете поне една снимка.")]
        public List<IFormFile> Images { get; set; }

        
        [Range(0, int.MaxValue, ErrorMessage = "Изберете валиден индекс за основното изображение.")]
        public int? MainImageIndex { get; set; }
    }
}

