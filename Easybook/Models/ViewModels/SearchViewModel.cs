using System.ComponentModel.DataAnnotations;

namespace Easybook.Models.ViewModels
{
    public class SearchViewModel
    {
        [Required(ErrorMessage = "Моля, изберете дата на настаняване.")]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Моля, изберете дата на напускане.")]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 10, ErrorMessage = "Моля, въведете валиден брой възрастни.")]
        public int Adults { get; set; }

        [Range(0, 10, ErrorMessage = "Моля, въведете валиден брой деца.")]
        public int Kids { get; set; }
    }

}
