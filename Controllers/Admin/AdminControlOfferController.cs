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
    public class AdminControlOfferController : ControllerBase
    {
        private readonly ClinicContext _context;

        public AdminControlOfferController(ClinicContext context)
        {
            _context = context;
        }

        // 1. Get all offers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Offer>>> GetOffers()
        {
            var offers = await _context.Offers.ToListAsync();
            return Ok(offers);
        }

        // 2. Get offer by Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Offer>> GetOffer(int id)
        {
            var offer = await _context.Offers.FindAsync(id);

            if (offer == null)
            {
                return NotFound(new { Message = "Offer not found" });
            }

            return Ok(offer);
        }

        // 3. Create a new offer
        [HttpPost]
        public async Task<ActionResult<Offer>> CreateOffer([FromBody] Offer offer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Ensure that start date is before end date
            if (offer.StartDate >= offer.EndDate)
            {
                return BadRequest(new { Message = "Start date must be before end date" });
            }

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOffer), new { id = offer.Id }, offer);
        }

        // 4. Update an offer
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOffer(int id, [FromBody] Offer offer)
        {
            if (id != offer.Id)
            {
                return BadRequest(new { Message = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(offer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Offers.Any(o => o.Id == id))
                {
                    return NotFound(new { Message = "Offer not found" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204 No Content - successful update
        }

        // 5. Delete an offer
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound(new { Message = "Offer not found" });
            }

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content - successful deletion
        }
    }
}
