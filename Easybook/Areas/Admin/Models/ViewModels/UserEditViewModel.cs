using Easybook.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Easybook.Areas.Admin.Models.ViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Моля, въведете име.")]
        [StringLength(50, ErrorMessage = "Името трябва да бъде максимум 50 символа.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Моля, въведете фамилия.")]
        [StringLength(50, ErrorMessage = "Фамилията трябва да бъде максимум 50 символа.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Моля, въведете имейл.")]
        [StrictEmailAddress(ErrorMessage = "Моля, въведете валиден имейл адрес с правилен домейн (например: example@domain.com).")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Моля, въведете телефонен номер.")]
        [ValidPhoneNumber(ErrorMessage = "Моля, въведете валиден телефонен номер с 10-12 цифри.")]
        public string PhoneNumber { get; set; }

        // For handling the profile picture upload
        public IFormFile? NewProfilePicture { get; set; }
        public string? ProfilePicture { get; set; }
    }
    public class ValidPhoneNumberAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            // Regex pattern for phone numbers: allows +, followed by 10 to 12 digits
            var phonePattern = @"^\+?[0-9]{10,12}$"; // At least 10 digits, no more than 12
            return value != null && Regex.IsMatch(value.ToString(), phonePattern);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Невалиден телефонен номер. Моля, въведете номер с 10-12 цифри.";
        }
    }
    public class StrictEmailAddressAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return false;

            // This regex pattern enforces a stricter validation for the email
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"; // Requires a valid domain extension

            return Regex.IsMatch(value.ToString(), emailPattern);
        }

        public override string FormatErrorMessage(string name)
        {
            return "Моля, въведете валиден имейл адрес с правилен домейн (например: example@domain.com).";
        }
    }
}
