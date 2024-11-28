using clinic_system.DTOs;
using clinic_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace clinic_system.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminControlSpecialtyController : ControllerBase
    {
        private readonly ClinicContext _context;

        public AdminControlSpecialtyController(ClinicContext context)
        {
            _context = context;
        }

        // 1. Get all specialties
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Specialty>>> GetSpecialties()
        {
            var specialties = await _context.Specialties.ToListAsync();
            return Ok(specialties);
        }

        // 2. Get specialty by Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Specialty>> GetSpecialty(int id)
        {
            var specialty = await _context.Specialties.FindAsync(id);

            if (specialty == null)
            {
                return NotFound(new { Message = "Specialty not found" });
            }

            return Ok(specialty);
        }

        // 3. Get specialty by Id
        [HttpGet("{name}")]
        public async Task<ActionResult<Specialty>> GetSpecialtyByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { Message = "Specialty name is required" });
            }

            var specialty = await _context.Specialties
                                          .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());

            if (specialty == null)
            {
                return NotFound(new { Message = "Specialty not found" });
            }

            return Ok(specialty);
        }

        // 4. Create new specialty
        [HttpPost]
        public async Task<ActionResult<Specialty>> CreateSpecialty([FromBody] SpecialtyDto createSpecialtyDto)
        {
            // Validate the incoming data
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Return validation errors
            }

            // Check if a specialty with the same name already exists
            var existingSpecialty = await _context.Specialties
                .FirstOrDefaultAsync(s => s.Name == createSpecialtyDto.Name);

            if (existingSpecialty != null)
            {
                return BadRequest(new { Message = "Specialty with the same name already exists" });
            }

            // Create a new Specialty entity
            var specialty = new Specialty
            {
                Name = createSpecialtyDto.Name,
                Description = createSpecialtyDto.Description
            };

            // Add the specialty to the database
            try
            {
                _context.Specialties.Add(specialty);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            // Return the created specialty
            return CreatedAtAction(nameof(GetSpecialty), new { id = specialty.Id }, specialty);
        }

        // 5. Update specialty
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSpecialty(int id, [FromBody] SpecialtyDto UpdateSpecialtyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var Specialty = await _context.Specialties.FindAsync(id);
            if (Specialty == null)
            {
                return NotFound(new { Message = "Specialty not found" });
            }


            Specialty.Name = UpdateSpecialtyDto.Name;
            Specialty.Description = UpdateSpecialtyDto.Description;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }


            return CreatedAtAction(nameof(GetSpecialty), new { id = Specialty.Id }, Specialty);
        }

        // 6. Delete specialty
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSpecialty(int id)
        {
            var specialty = await _context.Specialties.FindAsync(id);
            if (specialty == null)
            {
                return NotFound(new { Message = "Specialty not found" });
            }

            _context.Specialties.Remove(specialty);
            // Save changes to the database
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }


            return Ok(new { Message = "Delete successful" });
        }


        // 7.Get Doctors By Specialty
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
                .ToListAsync();

            if (doctors == null || !doctors.Any())
            {
                return NotFound(new { message = "No doctors found for this specialty." });
            }

            return Ok(doctors);
        }


        // Helper method to check if a specialty exists
        private bool SpecialtyExists(int id)
        {
            return _context.Specialties.Any(e => e.Id == id);
        }
    }

}
