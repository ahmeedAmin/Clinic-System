using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;

namespace clinic_system.Models
{
    public enum Gender
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        Male,
        [JsonConverter(typeof(JsonStringEnumConverter))]
        Female
    }
    public class Patient : User
    {
        [Range(0, int.MaxValue)]
        public int? Age { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]

        public Gender? Gender { get; set; }
        public string? Info {  get; set; }
        [ForeignKey("Doctor")]
        public int? DoctorId { get; set; }
        public virtual Doctor? Doctor { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Booking>? Bookings { get; set; } = new List<Booking>();
    }

}
