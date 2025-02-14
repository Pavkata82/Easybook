using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Easybook.Models.ViewModels
{
    public class BookingViewModel
    {

        [Required]
        public int HotelId { get; set; }

        [Required]
        public string ExactFitCombination { get; set; }

        [StringLength(100, ErrorMessage = "Това поле не може да бъде по-дълго от 100 символа.")]
        public string? SpecialRequests { get; set; } = "";

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [ValidPhoneNumber(ErrorMessage = "Невалиден телефонен номер.")]
        public string? PhoneNumber { get; set; }
    }

    public class ValidPhoneNumberAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            // You can define a regex pattern to check for more strict phone number validation
            var phonePattern = @"^\+?\d{10,12}$"; // At least 10 digits, no more than 12
            return value != null && Regex.IsMatch(value.ToString(), phonePattern);
        }
    }
}
