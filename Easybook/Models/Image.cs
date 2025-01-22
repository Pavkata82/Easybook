using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class Image
    {
        public int ImageId { get; set; }

        [Required]
        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }

        [Required]
        [DataType(DataType.ImageUrl)]
        public string ImageUrl { get; set; }

        public bool IsMain { get; set; }
    }

}
