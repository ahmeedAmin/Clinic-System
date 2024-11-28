using clinic_system.Models;
using System.ComponentModel.DataAnnotations;

namespace clinic_system.DTOs.AuthDtos
{
    public class RegisterPatientDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^(\+?\d{1,2}\s?)?(\(?\d{3}\)?[\s\-]?)?\d{3}[\s\-]?\d{4}$", ErrorMessage = "Invaled Phone Number...")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }

        public int? Age { get; set; }
        public Gender? Gender { get; set; }
        public string? Info { get; set; }

    }

}
