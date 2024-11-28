using clinic_system.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace clinic_system.Controllers.General
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeneralController : ControllerBase
    {
        private readonly ClinicContext _context;

        public GeneralController(ClinicContext context)
        {
            _context = context;
        }

        /////////////////////////////////////////Doctors///////////////////////////////////

        [HttpGet("GetDoctors")]
        public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            return Ok(doctors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Doctor>> GetDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
            {
                return NotFound(new { Message = "Doctor not found" });
            }

            return Ok(doctor);
        }

        [HttpGet("GetDoctorsBySpecialty/{specialtyId}")]
        public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctorsBySpecialty(int specialtyId)
        {
            var specialtyExists = await _context.Specialties
                .AnyAsync(s => s.Id == specialtyId);

            if (!specialtyExists)
            {
                return NotFound(new { message = "Specialty not found." });
            }

            var doctors = await _context.Doctors
                .Where(d => d.SpecialtyId == specialtyId)
                .Select(d => new
                {
                    Doctor = d,
                    AverageRating = _context.Reviews
                        .Where(r => r.DoctorId == d.Id)
                        .Average(r => (double?)r.Rating)
                })
                .OrderByDescending(d => d.AverageRating)
                .ToListAsync();

            if (!doctors.Any())
            {
                return NotFound(new { message = "No doctors found for this specialty." });
            }

            var doctorList = doctors.Select(d => d.Doctor).ToList();

            return Ok(doctorList);
        }

        [HttpGet("TopTenDoctors")]
        public async Task<ActionResult<IEnumerable<Doctor>>> TopTenDoctors()
        {
            var doctorsWithRatings = await _context.Doctors
                .Select(doctor => new
                {
                    Doctor = doctor,
                    AverageRating = _context.Reviews
                        .Where(r => r.DoctorId == doctor.Id)
                        .Average(r => r.Rating)
                })
                .ToListAsync();

            if (doctorsWithRatings.Any(d => d.AverageRating > 0))
            {
                var topDoctors = doctorsWithRatings
                    .Where(d => d.AverageRating > 0)
                    .OrderByDescending(d => d.AverageRating)
                    .Take(10)
                    .Select(d => d.Doctor)
                    .ToList();

                return Ok(topDoctors);
            }

            var doctorsWithoutRatings = await _context.Doctors
                .Take(10)
                .ToListAsync();

            return Ok(doctorsWithoutRatings);
        }

        /////////////////////////////////////////Specialties///////////////////////////////////
        [HttpGet("GetSpecialties")]
        public async Task<ActionResult<IEnumerable<Specialty>>> GetSpecialties()
        {
            var specialties = await _context.Specialties.ToListAsync();
            return Ok(specialties);
        }


        /////////////////////////////////////////Offers///////////////////////////////////
        [HttpGet("GetOffers")]
        public async Task<ActionResult<IEnumerable<Offer>>> GetOffers()
        {
            var offers = await _context.Offers.ToListAsync();
            return Ok(offers);
        }

        /////////////////////////////////////////reviews///////////////////////////////////
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
