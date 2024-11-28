using clinic_system.DTOs;
using clinic_system.DTOs.AuthDtos;
using clinic_system.DTOs.Booking;
using clinic_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;

namespace clinic_system.Controllers.Users
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Patient")]
    public class PatientController : ControllerBase
    {
        private readonly ClinicContext _clinicContext;

        public PatientController(ClinicContext clinicContext)
        {
            _clinicContext = clinicContext;
        }

        //////////////////////////////////////Profile///////////////////////////////////////////

        [HttpGet("Get-Me")]
        public async Task<ActionResult<Patient>> GetMe()
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var patient = await _clinicContext.Patients.FindAsync(patientId);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            return Ok(patient);
        }

        [HttpPut("Update-Me")]
        public async Task<ActionResult<Patient>> UpdateMe([FromBody] RegisterPatientDto updatePatientDto)
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var patient = await _clinicContext.Patients.FindAsync(patientId);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            if (!string.IsNullOrEmpty(updatePatientDto.Name))
            {
                patient.Name = updatePatientDto.Name;
            }

            if (!string.IsNullOrEmpty(updatePatientDto.Email))
            {
                patient.Email = updatePatientDto.Email;
            }

            if (!string.IsNullOrEmpty(updatePatientDto.PhoneNumber))
            {
                patient.PhoneNumber = updatePatientDto.PhoneNumber;
            }

            if (updatePatientDto.Age.HasValue)
            {
                patient.Age = updatePatientDto.Age.Value;
            }

            if (updatePatientDto.Gender.HasValue)
            {
                patient.Gender = updatePatientDto.Gender.Value;
            }

            if (!string.IsNullOrEmpty(updatePatientDto.Info))
            {
                patient.Info = updatePatientDto.Info;
            }

            try
            {
                _clinicContext.Patients.Update(patient);
                await _clinicContext.SaveChangesAsync();

                return Ok(new { message = "Patient information updated successfully", patient });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating patient information", error = ex.Message });
            }
        }

        [HttpDelete("Delete-Me")]
        public async Task<ActionResult> DeleteMe()
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var patient = await _clinicContext.Patients.FindAsync(patientId);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            try
            {
                _clinicContext.Patients.Remove(patient);
                await _clinicContext.SaveChangesAsync();

                return Ok(new { message = "Patient account deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the patient", error = ex.Message });
            }
        }

        //////////////////////////////////////Booking///////////////////////////////////////////
        [HttpGet("get-booking-by-id/{bookingId}")]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var booking = await _clinicContext.Bookings
             .Where(b => b.PatientId == patientId && b.Id == bookingId)
             .Select(b => new
             {
                 Booking = b,  
                 DoctorName = b.Doctor.Name,
                 sptialtyName = b.Doctor.Specialty.Name
             })
             .FirstOrDefaultAsync();


            if (booking == null)
            {
                return NotFound(new { message = "Booking not found with the given ID." });
            }

            return Ok(booking);
        }

        [HttpPost("Create-Booking")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto bookingDto)
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var patientName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var Doctor = await _clinicContext.Doctors.FindAsync(bookingDto.DoctorId);

            var booking = new Booking
            {
                DoctorId = bookingDto.DoctorId,
                PatientId = patientId,
                Date = bookingDto.Date,
                Day = bookingDto.Day,
                Time = bookingDto.Time,
                BookingStatus = BookingStatus.Pending,
                Amount = Doctor.ConsultationFee,
            };

            _clinicContext.Bookings.Add(booking);
            await _clinicContext.SaveChangesAsync();

            var notification = new Notification
            {
                Message = $"You have a new booking from patient {patientName}.",
                Date = DateTime.Now,
                ReceiverId = Doctor.Id,
                Type = NotificationType.Alert,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notification);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { Message = "Booking created successfully", BookingId = booking.Id });
        }

        [HttpPut("Update-Booking/{id}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] UpdateBookingDto bookingDto)
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var booking = await _clinicContext.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.PatientId == patientId);
            if (booking == null)
            {
                return NotFound(new { Message = "Booking not found" });
            }

            booking.Date = bookingDto.Date;
            booking.Day = bookingDto.Day;
            booking.Time = bookingDto.Time;

            _clinicContext.Bookings.Update(booking);
            await _clinicContext.SaveChangesAsync();

            var doctorId = booking.DoctorId;
            var doctor = await _clinicContext.Doctors.FindAsync(doctorId);

            var notification = new Notification
            {
                Message = $"Patient {User.FindFirstValue(ClaimTypes.Name)} has updated their booking.",
                Date = DateTime.Now,
                ReceiverId = doctorId,
                Type = NotificationType.Alert
            };

            _clinicContext.Notifications.Add(notification);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { Message = "Booking updated successfully" });
        }

        [HttpDelete("Delete-Booking/{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var booking = await _clinicContext.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.PatientId == patientId);
            if (booking == null)
            {
                return NotFound(new { Message = "Booking not found" });
            }

            _clinicContext.Bookings.Remove(booking);
            await _clinicContext.SaveChangesAsync();

            var doctorId = booking.DoctorId;
            var doctor = await _clinicContext.Doctors.FindAsync(doctorId);

            var notification = new Notification
            {
                Message = $"Patient {User.FindFirstValue(ClaimTypes.Name)} has cancelled their booking.",
                Date = DateTime.Now,
                ReceiverId = doctorId,
                Type = NotificationType.Alert
            };

            _clinicContext.Notifications.Add(notification);
            await _clinicContext.SaveChangesAsync();


            return Ok(new { Message = "Booking deleted successfully" });
        }

        [HttpGet("pending-Booking")]
        public async Task<IActionResult> GetPendingBookings()
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var pendingBookings = await _clinicContext.Bookings
                .Where(b => b.PatientId == patientId && b.BookingStatus == BookingStatus.Pending)
                .ToListAsync();

            return Ok(pendingBookings);
        }

        [HttpGet("confirmed-Booking")]
        public async Task<IActionResult> GetConfirmedBookings()
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var confirmedBookings = await _clinicContext.Bookings
                .Where(b => b.PatientId == patientId && b.BookingStatus == BookingStatus.Confirmed)
                .ToListAsync();

            return Ok(confirmedBookings);
        }

        [HttpGet("completed-Booking")]
        public async Task<IActionResult> GetCompletedBookings()
        {
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var completedBookings = await _clinicContext.Bookings
                .Where(b => b.PatientId == patientId && b.BookingStatus == BookingStatus.Completed)
                .ToListAsync();

            return Ok(completedBookings);
        }


        //////////////////////////////////////reviews///////////////////////////////////////////

        [HttpPost("add-review")]
        public async Task<IActionResult> AddReview(int doctorId, [FromBody] ReviewDto reviewDto)
        {
            if (reviewDto == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid review data." });
            }

            var doctorExists = await _clinicContext.Doctors.AnyAsync(d => d.Id == doctorId);
            if (!doctorExists)
            {
                return BadRequest(new { message = "Doctor not found." });
            }

            if (reviewDto.Rating < 1 || reviewDto.Rating > 5)
            {
                return BadRequest(new { message = "Rating must be between 1 and 5." });
            }

            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var patient = await _clinicContext.Patients.FindAsync(patientId);

            var review = new Review
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                ReviewDate = DateTime.Now
            };

            _clinicContext.Reviews.Add(review);

            var notification = new Notification
            {
                Message = $"You have received a new review from patient {patient.Name} with rating {reviewDto.Rating}.",
                Date = DateTime.Now,
                ReceiverId = doctorId,
                Type = NotificationType.Alert,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notification);

            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Review added successfully." });
        }

        [HttpPut("update-review/{reviewId}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] ReviewDto updatedReviewDto)
        {
            if (updatedReviewDto == null || updatedReviewDto.Rating < 1 || updatedReviewDto.Rating > 5)
            {
                return BadRequest(new { message = "Invalid review data." });
            }

            var review = await _clinicContext.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return NotFound(new { message = "Review not found." });
            }
            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var patient = await _clinicContext.Patients.FindAsync(patientId);

            if (review.PatientId != patientId)
            {
                return Unauthorized(new { message = "You can only update your own reviews." });
            }

            review.Rating = updatedReviewDto.Rating;
            review.Comment = updatedReviewDto.Comment;

            _clinicContext.Reviews.Update(review);
            await _clinicContext.SaveChangesAsync();

            var notification = new Notification
            {
                Message = $"Your review from patient {patient.Name} has been updated. Rating: {updatedReviewDto.Rating}",
                Date = DateTime.Now,
                ReceiverId = review.DoctorId,
                Type = NotificationType.Alert,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notification);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Review updated successfully." });
        }

        [HttpDelete("delete-review/{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var review = await _clinicContext.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return NotFound(new { message = "Review not found." });
            }

            var patientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out var patientId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            if (review.PatientId != patientId)
            {
                return Unauthorized(new { message = "You can only delete your own reviews." });
            }

            _clinicContext.Reviews.Remove(review);
            await _clinicContext.SaveChangesAsync();

            var notification = new Notification
            {
                Message = $"The review from patient {review.PatientId} has been deleted.",
                Date = DateTime.Now,
                ReceiverId = review.DoctorId,
                Type = NotificationType.Alert,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notification);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Review deleted successfully." });
        }

    }
}
