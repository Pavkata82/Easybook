using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int HotelId { get; set; }  // Foreign key to Hotel
        public Hotel? Hotel { get; set; }// Navigation property

        [Required]
        public string UserId { get; set; }  // Foreign key to ApplicationUser
        public ApplicationUser? User { get; set; } // Navigation property

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }  // Rating between 1 and 5

        [MaxLength(1000)]
        public string? Comment { get; set; }  // Optional comment

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Timestamp when the review was created
    }
}
