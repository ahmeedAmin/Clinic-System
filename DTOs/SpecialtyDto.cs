using System.ComponentModel.DataAnnotations;

namespace clinic_system.DTOs
{
    public class SpecialtyDto
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
