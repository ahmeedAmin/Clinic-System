using clinic_system.DTOs;
using clinic_system.DTOs.AuthDtos;
using clinic_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ClinicContext _clinicContext;
    private readonly IConfiguration _configuration;

    public AuthController(ClinicContext clinicContext, IConfiguration configuration)
    {
        _clinicContext = clinicContext;
        _configuration = configuration;
    }
    [HttpPost("register/admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminDto registerAdminDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingAdmin = await _clinicContext.Admins
            .FirstOrDefaultAsync(u => u.Email.ToLower() == registerAdminDto.Email.ToLower());
        if (existingAdmin != null)
        {
            return BadRequest(new { Message = "User with this email already exists." });
        }

        var refreshToken = GenerateRefreshToken();

        var admin = new Admin
        {
            Name = registerAdminDto.Name,
            Email = registerAdminDto.Email,
            PhoneNumber = registerAdminDto.Phone,
            PasswordHash = PasswordHelper.HashPassword(registerAdminDto.Password),
            Role = UserRole.Admin,
            RefreshToken = refreshToken
        };

        _clinicContext.Admins.Add(admin);

        try
        {
            await _clinicContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while saving the admin", Error = ex.Message });
        }

        var token = GenerateJwtToken(admin.Email, "Admin", admin.Id);

        return Ok(new
        {
            Message = "Admin registration successful",
            AdminId = admin.Id,
            Token = token,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("register/patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingPatient = await _clinicContext.Patients
            .AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());
        if (existingPatient)
        {
            return BadRequest(new { Message = "User with this email already exists." });
        }

        var refreshToken = GenerateRefreshToken();

        var patient = new Patient
        {
            Name = registerDto.Name,
            Email = registerDto.Email,
            PhoneNumber = registerDto.PhoneNumber,
            PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
            Role = UserRole.Patient,
            RefreshToken = refreshToken,
            Age = registerDto.Age,
            Gender = registerDto.Gender,
            Info = registerDto.Info ?? null
        };

        _clinicContext.Patients.Add(patient);
        await _clinicContext.SaveChangesAsync();

        var token = GenerateJwtToken(patient.Email, "Patient", patient.Id);

        return Ok(new
        {
            Message = "Patient registration successful",
            PatientId = patient.Id,
            Token = token,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("register/doctor")]
    public async Task<IActionResult> RegisterDoctor([FromForm] RegisterDoctorDto registerDoctorDto, IFormFile? profileImage)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var specialtyExists = await _clinicContext.Specialties.AnyAsync(s => s.Id == registerDoctorDto.SpecialtyId);
        if (!specialtyExists)
        {
            return BadRequest(new { Message = "The provided SpecialtyId does not exist." });
        }

        var existingDoctor = await _clinicContext.Doctors.AnyAsync(d => d.Email == registerDoctorDto.Email);
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

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                imageUrl = $"{Request.Scheme}://{Request.Host}/images/Doctors/{fileName}";
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"Error uploading image: {ex.Message}");
            }
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
            _clinicContext.Doctors.Add(doctor);
            await _clinicContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            return new BadRequestObjectResult($"An error occurred while saving the doctor: {ex.Message}");
        }

        var token = GenerateJwtToken(doctor.Email, "Doctor", doctor.Id);

        return Ok(new
        {
            Message = "Doctor registration successful",
            DoctorId = doctor.Id,
            Token = token,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        var patient = await _clinicContext.Patients.FirstOrDefaultAsync(p => p.Email == model.Email);
        var doctor = await _clinicContext.Doctors.FirstOrDefaultAsync(d => d.Email == model.Email);
        var admin = await _clinicContext.Admins.FirstOrDefaultAsync(a => a.Email == model.Email);

        if (patient == null && doctor == null && admin == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        string role = null;
        int userId = 0;

        if (patient != null && PasswordHelper.VerifyPassword(model.Password, patient.PasswordHash))
        {
            role = "Patient";
            userId = patient.Id;
        }
        else if (doctor != null && PasswordHelper.VerifyPassword(model.Password, doctor.PasswordHash))
        {
            role = "Doctor";
            userId = doctor.Id;
        }
        else if (admin != null && PasswordHelper.VerifyPassword(model.Password, admin.PasswordHash))
        {
            role = "Admin";
            userId = admin.Id;
        }
        else
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var token = GenerateJwtToken(model.Email, role, userId);

        var refreshToken = GenerateRefreshToken();

        if (patient != null)
        {
            patient.RefreshToken = refreshToken;
            _clinicContext.Patients.Update(patient);
        }
        else if (doctor != null)
        {
            doctor.RefreshToken = refreshToken;
            _clinicContext.Doctors.Update(doctor);
        }
        else if (admin != null)
        {
            admin.RefreshToken = refreshToken;
            _clinicContext.Admins.Update(admin);
        }

        await _clinicContext.SaveChangesAsync();

        return Ok(new LoginResponseDto
        {
            Message = "Login successful",
            Role = role,
            Token = token,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var patient = await _clinicContext.Patients.FirstOrDefaultAsync(p => p.RefreshToken == request.RefreshToken);
        var doctor = await _clinicContext.Doctors.FirstOrDefaultAsync(d => d.RefreshToken == request.RefreshToken);
        var admin = await _clinicContext.Admins.FirstOrDefaultAsync(a => a.RefreshToken == request.RefreshToken);

        if (patient == null && doctor == null && admin == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        try
        {
            if (patient != null)
            {
                patient.RefreshToken = null;
                _clinicContext.Patients.Update(patient);
            }
            else if (doctor != null)
            {
                doctor.RefreshToken = null;
                _clinicContext.Doctors.Update(doctor);
            }
            else if (admin != null)
            {
                admin.RefreshToken = null;
                _clinicContext.Admins.Update(admin);
            }

            await _clinicContext.SaveChangesAsync();

            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred during logout", error = ex.Message });
        }
    }

    private string GenerateJwtToken(string email, string role, int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var patient = await _clinicContext.Patients.FirstOrDefaultAsync(p => p.RefreshToken == request.RefreshToken);
        var doctor = await _clinicContext.Doctors.FirstOrDefaultAsync(d => d.RefreshToken == request.RefreshToken);
        var admin = await _clinicContext.Admins.FirstOrDefaultAsync(a => a.RefreshToken == request.RefreshToken);

        if (patient == null && doctor == null && admin == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        var email = patient?.Email ?? doctor?.Email ?? admin?.Email;
        var role = patient != null ? "Patient" : doctor != null ? "Doctor" : "Admin";
        var userId = patient?.Id ?? doctor?.Id ?? admin?.Id;

        var newJwtToken = GenerateJwtToken(email, role, userId.Value);

        var newRefreshToken = GenerateRefreshToken();

        if (patient != null)
        {
            patient.RefreshToken = newRefreshToken;
            _clinicContext.Patients.Update(patient);
        }
        else if (doctor != null)
        {
            doctor.RefreshToken = newRefreshToken;
            _clinicContext.Doctors.Update(doctor);
        }
        else if (admin != null)
        {
            admin.RefreshToken = newRefreshToken;
            _clinicContext.Admins.Update(admin);
        }

        try
        {
            await _clinicContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating the refresh token.", error = ex.Message });
        }

        return Ok(new { token = newJwtToken, refreshToken = newRefreshToken });
    }


}
