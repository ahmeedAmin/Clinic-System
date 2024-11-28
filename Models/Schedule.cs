using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinic_system.Models
{
    public class Schedule
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }  

        [Required]
        public TimeSpan StartTime { get; set; }  

        [Required]
        public TimeSpan EndTime { get; set; }  

        public bool IsAvailable { get; set; } = true;
    }

}
