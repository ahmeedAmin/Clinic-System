using System.ComponentModel.DataAnnotations;

namespace clinic_system.Models
{
    public class Specialty
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; } 

        public virtual ICollection<Doctor>? Doctors { get; set; }
    }

}
