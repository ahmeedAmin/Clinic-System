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
    public class AdminControlScheduleController : ControllerBase
    {
        private readonly ClinicContext _context;

        public AdminControlScheduleController(ClinicContext context)
        {
            _context = context;
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedulesByDoctorId(int doctorId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.DoctorId == doctorId)
                .ToListAsync();

            if (schedules == null || !schedules.Any())
            {
                return NotFound(new { Message = "No schedules found for this doctor" });
            }

            return Ok(schedules);
        }


        //Helper method to check if a schedule exists
        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }
    }
}
