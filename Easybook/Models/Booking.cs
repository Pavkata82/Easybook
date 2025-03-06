using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public string UserId { get; set; } // Foreign Key from AspNetUsers
        public ApplicationUser User { get; set; } // Navigation Property

        [Required]
        public int HotelId { get; set; } // Foreign Key to Hotel
        public Hotel Hotel { get; set; } // Navigation Property

        // Navigation property for BookingDateRanges
        public ICollection<BookingDateRange> BookingDateRanges { get; set; } = new List<BookingDateRange>();

        [Required]
        public int StatusId { get; set; } // E.g., Pending, Confirmed
        public Status Status { get; set; }

        [StringLength(100)]
        public string SpecialRequests { get; set; }

        // Add TotalPrice field
        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime DateOfBooking { get; set; } = DateTime.UtcNow;

        public bool IsPaid { get; set; } = false;
    }

}
