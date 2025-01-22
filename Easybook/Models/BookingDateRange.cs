using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class BookingDateRange
    {
        public int BookingDateRangeId { get; set; }

        [Required]
        public int RoomId { get; set; } // Foreign Key to Room
        public Room Room { get; set; } // Navigation Property

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        // New Foreign Key to Booking
        [Required]
        public int BookingId { get; set; } // Foreign Key to Booking
        public Booking Booking { get; set; } // Navigation Property
    }

}
