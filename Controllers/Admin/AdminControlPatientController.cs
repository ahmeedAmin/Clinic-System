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

    public class AdminControlPatientController : ControllerBase
    {
        private readonly ClinicContext _context;

        public AdminControlPatientController(ClinicContext context)
        {
            _context = context;
        }

        // 1. Get all patients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
            var patients = await _context.Patients.ToListAsync();
            return Ok(patients);
        }

        // 2. Get patient by Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
            {
                return NotFound(new { Message = "Patient not found" });
            }

            return Ok(patient);
        }

        // 3. Create new patient
        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] RegisterPatientDto patientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingPatient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == patientDto.Email);

            if (existingPatient != null)
            {
                return BadRequest(new { Message = "Patient with the same email already exists" });
            }

            var refreshToken = GenerateRefreshToken();
            var patient = new Patient
            {
                Name = patientDto.Name,
                Email = patientDto.Email,
                PhoneNumber = patientDto.PhoneNumber,
                PasswordHash = PasswordHelper.HashPassword(patientDto.Password),
                Role = UserRole.Patient,
                RefreshToken = refreshToken,
                Age = patientDto.Age,
                Gender = patientDto.Gender,
                Info = patientDto.Info ?? null
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Pationt created successful" });
        }

        // 4. Update patient
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] RegisterPatientDto patientDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingPationt = await _context.Patients.FirstOrDefaultAsync(d => d.Id == id);
            if (existingPationt == null)
            {
                return NotFound(new { Message = "Pationt not found." });
            }

            existingPationt.Name = patientDto.Name ?? existingPationt.Name;
            existingPationt.Email = patientDto.Email ?? existingPationt.Email;
            existingPationt.PhoneNumber = patientDto.PhoneNumber ?? existingPationt.PhoneNumber;
            existingPationt.Gender = patientDto.Gender ?? existingPationt.Gender;
            existingPationt.Info = patientDto.Info ?? existingPationt.Info;
            existingPationt.Age = patientDto.Age ?? existingPationt.Age;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(id))
                {
                    return NotFound(new { Message = "Patient not found" });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { Message = "Pationt Updated successful" });
        }

        // 5. Delete patient
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound(new { Message = "Patient not found" });
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content - successful deletion
        }

        // Helper method to check if a patient exists
        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
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
