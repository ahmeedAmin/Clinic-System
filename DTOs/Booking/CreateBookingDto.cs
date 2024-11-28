namespace clinic_system.DTOs.Booking
{
    public class CreateBookingDto
    {
        public int DoctorId { get; set; }
        public DateTime Date { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Time { get; set; }
    }
}
