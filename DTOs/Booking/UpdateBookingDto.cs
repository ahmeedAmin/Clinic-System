namespace clinic_system.DTOs.Booking
{
    public class UpdateBookingDto
    {
        public DateTime Date { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Time { get; set; }
    }
}
