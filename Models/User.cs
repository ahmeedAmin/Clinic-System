using System.ComponentModel.DataAnnotations;

namespace clinic_system.Models
{
    public enum UserRole
    {
        Patient,
        Doctor,
        Admin
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [RegularExpression(@"^(\+?\d{1,2}\s?)?(\(?\d{3}\)?[\s\-]?)?\d{3}[\s\-]?\d{4}$", ErrorMessage = "Invaled Phone Number...")]
        public string PhoneNumber { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string PasswordHash { get; set; } 
        public string RefreshToken { get; set; } 
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;  
        public virtual ICollection<Notification>? Notifications { get; set; } = new List<Notification>();
    }

}
