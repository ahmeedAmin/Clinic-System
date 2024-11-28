using clinic_system.DTOs.AuthDtos;
using clinic_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace clinic_system.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminControlDoctorController : ControllerBase
    {
        private readonly ClinicContext _context;

        public AdminControlDoctorController(ClinicContext context)
        {
            _context = context;
        }

        // 1. Get all doctors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            return Ok(doctors);
        }

        // 2. Get doctor by Id
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

        // 3. Create new doctor
        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromForm] RegisterDoctorDto registerDoctorDto, IFormFile? profileImage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var specialtyExists = await _context.Specialties.AnyAsync(s => s.Id == registerDoctorDto.SpecialtyId);
            if (!specialtyExists)
            {
                return BadRequest(new { Message = "The provided SpecialtyId does not exist." });
            }

            var existingDoctor = await _context.Doctors.AnyAsync(d => d.Email == registerDoctorDto.Email);
            if (existingDoctor)
            {
                return BadRequest(new { Message = "User with this email already exists." });
            }

            var refreshToken = GenerateRefreshToken();
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

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                imageUrl = $"{Request.Scheme}://{Request.Host}/images/Doctors/{fileName}";
            }

            var doctor = new Doctor
            {
                Name = registerDoctorDto.Name,
                Email = registerDoctorDto.Email,
                PasswordHash = PasswordHelper.HashPassword(registerDoctorDto.Password),
                SpecialtyId = registerDoctorDto.SpecialtyId,
                ConsultationFee = registerDoctorDto.ConsultationFee,
                PhoneNumber = registerDoctorDto.PhoneNumber,
                Role = UserRole.Doctor,
                RefreshToken = refreshToken,
                Gender = registerDoctorDto.Gender,
                Info = registerDoctorDto.Info,
                Experience = registerDoctorDto.Experience,
                ProfileImageUrl = imageUrl ?? registerDoctorDto.ProfileImageUrl
            };

            try
            {
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return new BadRequestObjectResult($"An error occurred while saving the doctor: {ex.Message}");
            }

            return Ok(new { Message = "Doctor created successful" });
        }

        // 4. Update doctor
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromForm] RegisterDoctorDto registerDoctorDto, IFormFile? profileImage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var existingDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id);
            if (existingDoctor == null)
            {
                return NotFound(new { Message = "Doctor not found." });
            }

            var specialtyExists = await _context.Specialties.AnyAsync(s => s.Id == registerDoctorDto.SpecialtyId);
            if (!specialtyExists)
            {
                return BadRequest(new { Message = "The provided SpecialtyId does not exist." });
            }

            existingDoctor.Name = registerDoctorDto.Name ?? existingDoctor.Name;
            existingDoctor.Email = registerDoctorDto.Email ?? existingDoctor.Email;
            existingDoctor.PhoneNumber = registerDoctorDto.PhoneNumber ?? existingDoctor.PhoneNumber;
            existingDoctor.SpecialtyId = registerDoctorDto.SpecialtyId;
            existingDoctor.ConsultationFee = registerDoctorDto.ConsultationFee != 0 ? registerDoctorDto.ConsultationFee : existingDoctor.ConsultationFee;
            existingDoctor.Gender = registerDoctorDto.Gender ?? existingDoctor.Gender;
            existingDoctor.Info = registerDoctorDto.Info ?? existingDoctor.Info;
            existingDoctor.Experience = registerDoctorDto.Experience != 0 ? registerDoctorDto.Experience : existingDoctor.Experience;

            if (profileImage != null)
            {
                var fileName = Path.GetFileNameWithoutExtension(profileImage.FileName)
                               + "_" + Guid.NewGuid().ToString()
                               + Path.GetExtension(profileImage.FileName);
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "Doctors");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var filePath = Path.Combine(directoryPath, fileName);

                // Delete existing profile image (if exists) before saving the new one
                if (!string.IsNullOrEmpty(existingDoctor.ProfileImageUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "Doctors", Path.GetFileName(existingDoctor.ProfileImageUrl));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save the new profile image
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                existingDoctor.ProfileImageUrl = $"{Request.Scheme}://{Request.Host}/images/Doctors/{fileName}";
            }

            // Step 5: Save the changes to the database
            _context.Doctors.Update(existingDoctor);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Doctor Updated successful" });
        }

        // 5. Delete doctor
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound(new { Message = "Doctor not found" });
            }

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper method to check if a doctor exists
        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

    }
}
