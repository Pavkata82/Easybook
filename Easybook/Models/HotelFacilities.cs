namespace Easybook.Models
{
    public class HotelFacilities
    {
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; } // Navigation property

        public int FacilityId { get; set; }
        public Facility Facility { get; set; } // Navigation property
    }

}
