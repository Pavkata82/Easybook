using System.ComponentModel.DataAnnotations;

namespace Easybook.Models.ViewModels
{
    public class HotelViewModel
    {
        public int HotelId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [DataType(DataType.ImageUrl)]
        public string ImageUrl { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        public List<string> RoomTypes { get; set; }

        public List<string> Images { get; set; } = new List<string>();
    }


}
