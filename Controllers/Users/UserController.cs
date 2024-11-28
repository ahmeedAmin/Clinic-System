using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using clinic_system.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace clinic_system.Controllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "PatientOrDoctor")]

    public class UserController : ControllerBase
    {
        private readonly ClinicContext _clinicContext;

        public UserController(ClinicContext clinicContext)
        {
            _clinicContext = clinicContext;
        }

        [HttpPost("send-notification")]
        public async Task<IActionResult> SendNotification([FromBody] Notification notification)
        {
            if (notification == null || string.IsNullOrEmpty(notification.Message))
            {
                return BadRequest(new { message = "Notification message is required." });
            }

            var UserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(UserIdClaim) || !int.TryParse(UserIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            if (userId != notification.ReceiverId)
            {
                return BadRequest(new { message = "Notification receiver mismatch." });
            }

            notification.Date = DateTime.Now;
            _clinicContext.Notifications.Add(notification);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Notification sent successfully." });
        }

        [HttpGet("GetMyNotifications")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetMyDoctorNotifications()
        {
            var UserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(UserIdClaim) || !int.TryParse(UserIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var notifications = await _clinicContext.Notifications
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.Date)
                .ToListAsync();

            if (notifications == null || !notifications.Any())
            {
                return NotFound(new { message = "No notifications found." });
            }

            return Ok(notifications);
        }

        [HttpGet("GetUnreadNotifications")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUnreadDoctorNotifications()
        {
            var UserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(UserIdClaim) || !int.TryParse(UserIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }


            var unreadNotifications = await _clinicContext.Notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .OrderByDescending(n => n.Date)
                .ToListAsync();

            if (unreadNotifications == null || !unreadNotifications.Any())
            {
                return NotFound(new { message = "No unread notifications found." });
            }

            return Ok(unreadNotifications);
        }
        [HttpPut("mark-notification-read/{notificationId}")]
        public async Task<IActionResult> MarkNotificationRead(int notificationId)
        {
            var notification = await _clinicContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                return NotFound(new { message = "Notification not found." });
            }

            notification.IsRead = true;
            notification.DateRead = DateTime.Now;

            _clinicContext.Notifications.Update(notification);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read." });
        }

        [HttpDelete("DeleteNotification/{id}")]
        public async Task<ActionResult> DeleteDoctorNotification(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var notification = await _clinicContext.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound(new { message = "Notification not found." });
            }

            if (notification.ReceiverId != userId)
            {
                return Unauthorized(new { message = "You are not authorized to delete this notification." });
            }

            _clinicContext.Notifications.Remove(notification);

            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Notification deleted successfully." });
        }

    }
}
