using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TimeTrackerAPI.Data;
using TimeTrackerAPI.Models;
using TimeTrackerAPI.Services;

namespace TimeTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                _logger.LogInformation("LocalLogin method called.");

                // Validate username
                if (string.IsNullOrWhiteSpace(model.Login) || model.Login.Length < 4 || model.Login.Length > 20)
                {
                    _logger.LogWarning("Invalid username length.");
                    return BadRequest(new { ErrorCode = "InvalidUsernameLength", Message = "Username must be between 4 and 20 characters." });
                }

                // Validate password
                if (string.IsNullOrWhiteSpace(model.Password) || !Regex.IsMatch(model.Password, @"^(?=.*[0-9])[a-z0-9]{8,}$"))
                {
                    _logger.LogWarning("Invalid password format.");
                    return BadRequest(new { ErrorCode = "InvalidPasswordFormat", Message = "Password must be at least 8 characters long, contain only lowercase Latin letters, and include at least one number." });
                }

                var login = model.Login.ToLowerInvariant();
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Login.ToLower() == login);

                if (user == null)
                {
                    _logger.LogWarning("User not found.");
                    return Unauthorized(new { ErrorCode = "UserNotFound", Message = "Invalid username." });
                }

                bool isPasswordValid = SHA512Hasher.VerifyPassword(model.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Invalid password.");
                    return Unauthorized(new { ErrorCode = "InvalidPassword", Message = "Invalid password." });
                }

                var token = GenerateJwtToken(user);
                return Ok(new { Message = "Access granted for local authenticated user.", Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing LocalLogin.");
                return StatusCode(500, new { ErrorCode = "InternalServerError", Message = "Internal server error" });
            }
        }


        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Login),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("EmployeeId", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
