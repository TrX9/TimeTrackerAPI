using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.IdentityModel.Tokens;
using TimeTrackerAPI.Data;
using TimeTrackerAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using TimeTrackerAPI.Services;
using Microsoft.Identity.Web.Resource;
using System.Text.RegularExpressions;

namespace TimeTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<AuthController> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ITokenAcquisition tokenAcquisition,
            ILogger<AuthController> logger, GraphServiceClient graphServiceClient)
        {
            _context = context;
            _configuration = configuration;
            _tokenAcquisition = tokenAcquisition;
            _logger = logger;
            _graphServiceClient = graphServiceClient;
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
                    return BadRequest(new { Message = "Username must be between 4 and 20 characters." });
                }

                // Validate password
                if (string.IsNullOrWhiteSpace(model.Password) || !Regex.IsMatch(model.Password, @"^(?=.*[0-9])[a-z0-9]{8,}$"))
                {
                    _logger.LogWarning("Invalid password format.");
                    return BadRequest(new { Message = "Password must be at least 8 characters long, contain only lowercase Latin letters, and include at least one number." });
                }

                var login = model.Login.ToLowerInvariant();
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Login.ToLower() == login);

                if (user == null)
                {
                    _logger.LogWarning("User not found.");
                    return Unauthorized(new { Message = "Invalid username." });
                }

                bool isPasswordValid = SHA512Hasher.VerifyPassword(model.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Invalid password.");
                    return Unauthorized(new { Message = "Invalid password." });
                }

                var token = GenerateJwtToken(user);
                return Ok(new { Message = "Access granted for local authenticated user.", Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing LocalLogin.");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("login-azure")]
        [Authorize(AuthenticationSchemes = "AzureAd")]
        public async Task<IActionResult> AzureLogin()
        {
            var user = await _graphServiceClient.Me.GetAsync();

            // Check if the user is in the Azure AD directory
            var directoryUser = await _graphServiceClient.Users[user.Id].GetAsync();

            if (directoryUser != null)
            {
                // Get an access token to call downstream APIs (if needed)
                var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read" });
                return Ok(new { Token = token });
            }

            return Unauthorized("User is not present in the directory.");
        }

        [HttpGet("signin-oidc")]
        [Authorize(AuthenticationSchemes = "AzureAd")]
        public async Task<IActionResult> SignInOidc()
        {
            // Handle the redirect from Azure AD and acquire tokens
            var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            var accessToken = result.Properties.GetTokenValue("access_token");
            if (accessToken == null)
            {
                return Unauthorized();
            }

            // You might want to add additional logic to check if the user exists in Azure AD if needed
            // e.g., check user claims or roles

            return Ok(new { AccessToken = accessToken });
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
