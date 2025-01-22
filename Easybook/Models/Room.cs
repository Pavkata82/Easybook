using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        [Required]
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }

        [Required]
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }

        [Required]
        [Range(1, 10)]
        public int Capacity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public ICollection<BookingDateRange> BookingDateRanges { get; set; }
    }


}
