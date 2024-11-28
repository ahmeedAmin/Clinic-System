using clinic_system.Models;
using clinic_system.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using clinic_system.DTOs.Booking;
using clinic_system.DTOs.AuthDtos;

namespace clinic_system.Controllers.Users
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class DoctorController : ControllerBase
    {
        private readonly ClinicContext _clinicContext;

        public DoctorController(ClinicContext clinicContext)
        {
            _clinicContext = clinicContext;
        }

        //////////////////////////////////////profile///////////////////////////////////////////

        [HttpGet("Get-Me")]
        public async Task<ActionResult<Doctor>> GetMe()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var DoctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var Doctor = await _clinicContext.Doctors.FindAsync(DoctorId);

            if (Doctor == null)
            {
                return NotFound(new { message = "Doctor not found" });
            }

            return Ok(Doctor);
        }

        [HttpPut("Update-Me")]
        public async Task<IActionResult> UpdateMe([FromForm] RegisterDoctorDto updateDoctorDto, IFormFile? profileImage)
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var doctor = await _clinicContext.Doctors.FindAsync(doctorId);

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            if (updateDoctorDto.SpecialtyId > 0)
            {
                var specialtyExists = await _clinicContext.Specialties.AnyAsync(s => s.Id == updateDoctorDto.SpecialtyId);
                if (!specialtyExists)
                {
                    return BadRequest(new { Message = "The provided SpecialtyId does not exist." });
                }
            }

            if (!string.IsNullOrEmpty(updateDoctorDto.Email) && updateDoctorDto.Email != doctor.Email)
            {
                var existingDoctor = await _clinicContext.Doctors.AnyAsync(d => d.Email == updateDoctorDto.Email);
                if (existingDoctor)
                {
                    return BadRequest(new { Message = "A doctor with this email already exists." });
                }
            }

            doctor.Name = string.IsNullOrEmpty(updateDoctorDto.Name) ? doctor.Name : updateDoctorDto.Name;
            doctor.Email = string.IsNullOrEmpty(updateDoctorDto.Email) ? doctor.Email : updateDoctorDto.Email;
            doctor.PhoneNumber = string.IsNullOrEmpty(updateDoctorDto.PhoneNumber) ? doctor.PhoneNumber : updateDoctorDto.PhoneNumber;
            doctor.ConsultationFee = updateDoctorDto.ConsultationFee > 0 ? updateDoctorDto.ConsultationFee : doctor.ConsultationFee;
            doctor.Experience = updateDoctorDto.Experience > 0 ? updateDoctorDto.Experience : doctor.Experience;
            doctor.Info = string.IsNullOrEmpty(updateDoctorDto.Info) ? doctor.Info : updateDoctorDto.Info;
            doctor.Gender = updateDoctorDto.Gender ?? doctor.Gender;
            doctor.SpecialtyId = updateDoctorDto.SpecialtyId > 0 ? updateDoctorDto.SpecialtyId : doctor.SpecialtyId;
            doctor.ProfileImageUrl = string.IsNullOrEmpty(updateDoctorDto.ProfileImageUrl) ? doctor.ProfileImageUrl : updateDoctorDto.ProfileImageUrl;

            string filePath = null;
            string imageUrl = null;

            if (profileImage != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profileImage.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new BadRequestObjectResult("Only image files (jpg, jpeg, png, gif) are allowed.");
                }

                var fileName = Path.GetFileNameWithoutExtension(profileImage.FileName)
                                   + "_" + Guid.NewGuid().ToString()
                                   + Path.GetExtension(profileImage.FileName);

                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "Doctors");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                filePath = Path.Combine(directoryPath, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    imageUrl = $"{Request.Scheme}://{Request.Host}/images/Doctors/{fileName}";
                    doctor.ProfileImageUrl = imageUrl;
                }
                catch (Exception ex)
                {
                    return new BadRequestObjectResult($"Error uploading image: {ex.Message}");
                }
            }

            try
            {
                _clinicContext.Doctors.Update(doctor);
                await _clinicContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return new BadRequestObjectResult($"An error occurred while updating doctor: {ex.Message}");
            }

            return Ok(new
            {
                Message = "Doctor information updated successfully",
                DoctorId = doctor.Id,
                UpdatedDoctor = doctor
            });
        }

        [HttpDelete("Delete-Me")]
        public async Task<IActionResult> DeleteMe()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var doctor = await _clinicContext.Doctors.FindAsync(doctorId);

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }


            _clinicContext.Doctors.Remove(doctor);

            try
            {
                await _clinicContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the doctor", error = ex.Message });
            }

            return Ok(new { message = "Doctor deleted successfully." });
        }


        //////////////////////////////////////Booking///////////////////////////////////////////


        [HttpGet("get-booking-by-inspection-number")]
        public async Task<IActionResult> GetBookingByInspectionNumber(int inspectionNumber)
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var today = DateTime.Now.Date;

            var booking = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.Date.Date == today && b.InspectionNumber == inspectionNumber)
                .Include(b => b.Patient)
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found for the given inspection number." });
            }

            return Ok(booking);
        }

        [HttpGet("get-booking-by-id/{bookingId}")]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var booking = await _clinicContext.Bookings
             .Where(b => b.DoctorId == doctorId && b.Id == bookingId)
             .Select(b => new
             {
                 Booking = b,
                 PatientName = b.Patient.Name,
                 sptialtyName = b.Doctor.Specialty.Name
             })
             .FirstOrDefaultAsync();

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found with the given ID." });
            }

            return Ok(booking);
        }

        [HttpGet("get-My-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var bookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId)
                .Include(b => b.Patient)
                .ToListAsync();

            if (bookings == null || bookings.Count == 0)
            {
                return NotFound(new { message = "No bookings found for this doctor." });
            }

            return Ok(bookings);
        }

        [HttpGet("get-pending-bookings")]
        public async Task<IActionResult> GetPendingBookings()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var bookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Pending)
                .Include(b => b.Patient)
                .ToListAsync();

            if (bookings == null || bookings.Count == 0)
            {
                return NotFound(new { message = "No pending bookings found." });
            }

            return Ok(bookings);
        }

        [HttpGet("get-confirmed-bookings")]
        public async Task<IActionResult> GetConfirmedBookings()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var bookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Confirmed)
                .Include(b => b.Patient)
                .ToListAsync();

            if (bookings == null || bookings.Count == 0)
            {
                return NotFound(new { message = "No confirmed bookings found." });
            }

            return Ok(bookings);
        }

        [HttpGet("get-completed-bookings")]
        public async Task<IActionResult> GetCompletedBookings()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var bookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Completed)
                .Include(b => b.Patient)
                .ToListAsync();

            if (bookings == null || bookings.Count == 0)
            {
                return NotFound(new { message = "No completed bookings found." });
            }

            return Ok(bookings);
        }

        [HttpGet("get-cancelled-bookings")]
        public async Task<IActionResult> GetCancelledBookings()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var bookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Cancelled)
                .Include(b => b.Patient)
                .ToListAsync();

            if (bookings == null || bookings.Count == 0)
            {
                return NotFound(new { message = "No cancelled bookings found." });
            }

            return Ok(bookings);
        }

        [HttpPut("confirm-booking/{bookingId}")]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var doctorName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var booking = await _clinicContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.DoctorId == doctorId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found or does not belong to this doctor." });
            }

            if (booking.BookingStatus == BookingStatus.Confirmed)
            {
                return BadRequest(new { message = "Booking is already confirmed." });
            }

            booking.BookingStatus = BookingStatus.Confirmed;
            var today = DateTime.Now.Date;
            var maxInspectionNumber = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId
                            && b.BookingStatus == BookingStatus.Confirmed
                            || b.BookingStatus == BookingStatus.Completed
                            && b.Date.Date == today)
                .MaxAsync(b => b.InspectionNumber) ?? 0;

            booking.InspectionNumber = maxInspectionNumber + 1;

            var notificationForPatient = new Notification
            {
                Message = $"Your booking with doctor {doctorName} has been confirmed.",
                Date = DateTime.Now,
                ReceiverId = booking.PatientId,
                Type = NotificationType.Confirmation,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notificationForPatient);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Booking confirmed successfully." });
        }

        [HttpPut("cancel-booking/{bookingId}")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var doctorName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var booking = await _clinicContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.DoctorId == doctorId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found or does not belong to this doctor." });
            }

            if (booking.BookingStatus == BookingStatus.Cancelled)
            {
                return BadRequest(new { message = "Booking is already cancelled." });
            }

            booking.BookingStatus = BookingStatus.Cancelled;

            var notificationForPatient = new Notification
            {
                Message = $"Your booking with doctor {doctorName} has been cancelled.",
                Date = DateTime.Now,
                ReceiverId = booking.PatientId,
                Type = NotificationType.Warning,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notificationForPatient);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Booking cancelled successfully." });
        }

        [HttpPut("complete-booking/{bookingId}")]
        public async Task<IActionResult> CompleteBooking(int bookingId, [FromForm] CompleteBooking completeBooking)
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var doctorName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var booking = await _clinicContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.DoctorId == doctorId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found or does not belong to this doctor." });
            }

            if (booking.BookingStatus == BookingStatus.Completed)
            {
                return BadRequest(new { message = "Booking is already completed." });
            }

            booking.BookingStatus = BookingStatus.Completed;
            booking.Diagnosis = completeBooking.Diagnosis;
            booking.Prescription = completeBooking.Prescription;
            var notificationForPatient = new Notification
            {
                Message = $"Your booking with doctor {doctorName} has been completed.",
                Date = DateTime.Now,
                ReceiverId = booking.PatientId,
                Type = NotificationType.Alert,
                IsRead = false
            };

            _clinicContext.Notifications.Add(notificationForPatient);
            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Booking completed successfully." });
        }

        [HttpGet("get-todays-bookings")]
        public async Task<IActionResult> GetTodaysBookings()
        {
            var DoctorIdIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(DoctorIdIdClaim) || !int.TryParse(DoctorIdIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var today = DateTime.Today;

            var completedBookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.Date.Date == today && b.BookingStatus == BookingStatus.Completed)
                .Include(b => b.Patient)
                .ToListAsync();

            var pendingBookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.Date.Date == today && b.BookingStatus == BookingStatus.Pending)
                .Include(b => b.Patient)
                .ToListAsync();

            var waitingBookings = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.Date.Date == today && b.BookingStatus == BookingStatus.Cancelled)
                .Include(b => b.Patient)
                .ToListAsync();

            return Ok(new
            {
                CompletedBookings = completedBookings,
                PendingBookings = pendingBookings,
                WaitingBookings = waitingBookings
            });
        }


        //////////////////////////////////////Reviews///////////////////////////////////////////
        [HttpGet("GetMyReviews")]
        public async Task<ActionResult<IEnumerable<Review>>> GetMyReviews()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var reviews = await _clinicContext.Reviews
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            if (reviews == null || !reviews.Any())
            {
                return NotFound(new { message = "No reviews found for this doctor." });
            }

            return Ok(reviews);
        }

        [HttpGet("GetMyRating")]
        public async Task<IActionResult> GetMyRating()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var reviews = await _clinicContext.Reviews
                .Where(r => r.DoctorId == doctorId)
                .ToListAsync();

            if (reviews == null || reviews.Count == 0)
            {
                return NotFound(new { message = "No reviews found for this doctor." });
            }

            var averageRating = reviews
                .Where(r => r.Rating.HasValue)
                .Average(r => r.Rating.Value);

            var roundedAverageRating = (int)Math.Round(averageRating);

            return Ok(new { AverageRating = roundedAverageRating });
        }


        //////////////////////////////////////Schedules///////////////////////////////////////////

        [HttpGet("GetMySchedules")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetMySchedules()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var schedules = await _clinicContext.Schedules
                .Where(s => s.DoctorId == doctorId)
                .ToListAsync();

            if (schedules == null || !schedules.Any())
            {
                return NotFound(new { message = "No schedules found for this doctor." });
            }

            return Ok(schedules);
        }
        [HttpPost("create-schedule")]
        public async Task<IActionResult> CreateSchedule([FromBody] ScheduleDto scheduleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var newSchedule = new Schedule
            {
                DoctorId = doctorId,  
                Day = scheduleDto.Day,
                StartTime = scheduleDto.StartTime,
                EndTime = scheduleDto.EndTime
            };

            _clinicContext.Schedules.Add(newSchedule);

            try
            {
                await _clinicContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the schedule", error = ex.Message });
            }

            return Ok(new { message = "Schedule created successfully", schedule = newSchedule });
        }


        [HttpPut("UpdateMySchedule/{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleDto scheduleDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var schedule = await _clinicContext.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound(new { Message = "Schedule not found" });
            }

            if (schedule.DoctorId != doctorId)
            {
                return Unauthorized(new { message = "You are not authorized to update this schedule." });
            }

            schedule.Day = scheduleDto.Day;
            schedule.StartTime = scheduleDto.StartTime;
            schedule.EndTime = scheduleDto.EndTime;

            try
            {
                await _clinicContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the schedule", error = ex.Message });
            }

            return Ok(new { message = "Schedule updated successfully", schedule });
        }

        [HttpDelete("DeleteMySchedule/{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var schedule = await _clinicContext.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return NotFound(new { Message = "Schedule not found" });
            }

            if (schedule.DoctorId != doctorId)
            {
                return Unauthorized(new { message = "You are not authorized to delete this schedule." });
            }

            try
            {
                _clinicContext.Schedules.Remove(schedule);

                await _clinicContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the schedule", error = ex.Message });
            }

            return Ok(new { message = "Schedule deleted successfully" });
        }

        //////////////////////////////////////reports///////////////////////////////////////////
        [HttpGet("GetTotalBookings")]
        public async Task<ActionResult<int>> GetTotalBookings()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var doctorExists = await _clinicContext.Doctors.AnyAsync(d => d.Id == doctorId);
            if (!doctorExists)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            var totalBookings = await _clinicContext.Bookings
                .CountAsync(b => b.DoctorId == doctorId);

            return Ok(new { TotalBookings = totalBookings });
        }
        [HttpGet("GetCountCompletedBookings")]
        public async Task<ActionResult<int>> GetCountCompletedBookings()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var completedBookingsCount = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Completed)
                .CountAsync();

            return Ok(new { CompletedBookings = completedBookingsCount });
        }
        [HttpGet("GetCountConfirmedBookings")]
        public async Task<ActionResult<int>> GetCountConfirmedBookings()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }

            var confirmedBookingsCount = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Confirmed)
                .CountAsync();

            return Ok(new { ConfirmedBookings = confirmedBookingsCount });
        }
        [HttpGet("GetCountPendingBookings")]
        public async Task<ActionResult<int>> GetCountPendingBookings()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var pendingBookingsCount = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Pending)
                .CountAsync();

            return Ok(new { PendingBookings = pendingBookingsCount });
        }
        [HttpGet("GetCountCancelledBookings")]
        public async Task<ActionResult<int>> GetCountCancelledBookings()
        {
            var doctorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(doctorIdClaim) || !int.TryParse(doctorIdClaim, out var doctorId))
            {
                return Unauthorized(new { message = "Invalid or missing user information." });
            }
            var cancelledBookingsCount = await _clinicContext.Bookings
                .Where(b => b.DoctorId == doctorId && b.BookingStatus == BookingStatus.Cancelled)
                .CountAsync();

            return Ok(new { CancelledBookings = cancelledBookingsCount });
        }

    }
}
