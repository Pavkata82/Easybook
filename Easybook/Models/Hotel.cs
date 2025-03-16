using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace Easybook.Models
{
    public class Hotel
    {
        public int HotelId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();

        public ICollection<HotelFacilities> HotelFacilities { get; set; }

        public ICollection<Image> Images { get; set; } = new List<Image>();

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public bool IsActive { get; set; } = true; // Default is active
    }

}
