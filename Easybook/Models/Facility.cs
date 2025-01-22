using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class Facility
    {
        public int FacilityId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Navigation property for the many-to-many relationship
        public ICollection<HotelFacilities> HotelFacilities { get; set; }
    }

}
