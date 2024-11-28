using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinic_system.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [Range(1,5)]
        public int? Rating { get; set; } 
        public string? Comment { get; set; }   
        public DateTime ReviewDate { get; set; }
    }

}
