using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace clinic_system.Models
{
    public enum BookingStatus
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        Pending,
        [JsonConverter(typeof(JsonStringEnumConverter))]
        Confirmed,
        [JsonConverter(typeof(JsonStringEnumConverter))]
        Cancelled,
        [JsonConverter(typeof(JsonStringEnumConverter))]
        Completed
    }
    public class Booking
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public DateTime Date { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Time { get; set; }
        public double Amount { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BookingStatus BookingStatus { get; set; }
        public int? InspectionNumber {get;set; }

        public string? Diagnosis { get; set; }
        public string? Prescription { get; set; }
    }
}
