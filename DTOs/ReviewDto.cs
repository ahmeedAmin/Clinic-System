using System.ComponentModel.DataAnnotations;

namespace clinic_system.DTOs
{
    public class ReviewDto
    {
        [Range(1, 5)]
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }
}
