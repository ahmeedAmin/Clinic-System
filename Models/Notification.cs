using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace clinic_system.Models
{
    public enum NotificationType
    {
        Alert,
        Confirmation,
        Reminder,
        Warning
    }

    public class Notification
    {
        public int Id { get; set; }

        public string Message { get; set; }

        public DateTime Date { get; set; }

        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? DateRead { get; set; }

        public NotificationType Type { get; set; }
    }
}
