using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace clinic_system.Models
{
    public class Doctor : User
    {
        [Required]
        [Range(0, int.MaxValue)]
        public double ConsultationFee { get; set; }
        [Range(0, int.MaxValue)]
        public int Experience { get; set; }
        public string? Info { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]

        public Gender? Gender { get; set; } 
        public string? ProfileImageUrl { get; set; }
        public List<Schedule>? Schedules { get; set; }
        [ForeignKey("Specialty")]
        public int SpecialtyId { get; set; }
        public virtual Specialty? Specialty { get; set; }
        public virtual ICollection<Patient>? Patients { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();
    }

}
