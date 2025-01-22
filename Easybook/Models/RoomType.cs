using System.ComponentModel.DataAnnotations;

namespace Easybook.Models
{
    public class RoomType
    {
        public int RoomTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // E.g., Single, Double, Family

        public ICollection<Room> Rooms { get; set; }
    }

}
