using clinic_system.DTOs;
using clinic_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace clinic_system.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]


    public class AdminControlReviewController : ControllerBase
    {
        private readonly ClinicContext _context;

        public AdminControlReviewController(ClinicContext clinicContext)
        {
            _context = clinicContext;
        }

        [HttpGet("get-doctor-reviews/{doctorId}")]
        public async Task<IActionResult> GetDoctorReviews(int doctorId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.DoctorId == doctorId)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            if (reviews == null || reviews.Count == 0)
            {
                return NotFound(new { message = "No reviews found for this doctor." });
            }

            return Ok(reviews);
        }

        [HttpGet("get-patient-reviews/{patientId}")]
        public async Task<IActionResult> GetPatientReviews(int patientId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.PatientId == patientId)
                .Include(r => r.Doctor)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            if (reviews == null || reviews.Count == 0)
            {
                return NotFound(new { message = "No reviews found for this patient." });
            }

            return Ok(reviews);
        }

        [HttpDelete("delete-review/{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return NotFound(new { message = "Review not found." });
            }

            var patientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (review.PatientId != patientId)
            {
                return Unauthorized(new { message = "You can only delete your own reviews." });
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review deleted successfully." });
        }

        [HttpGet("get-doctor-rating/{doctorId}")]
        public async Task<IActionResult> GetDoctorRating(int doctorId)
        {
            var reviews = await _context.Reviews
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

    }
}
