using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        // Add the ProfilePictureUrl property
        public string ProfilePictureUrl { get; set; } = "/images/users/default.jpg";

        // Navigation property to the list of bookings by the user
        public ICollection<Booking> Bookings { get; set; }
    }
}
