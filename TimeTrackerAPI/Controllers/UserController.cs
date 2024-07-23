using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackerAPI.Data;
using TimeTrackerAPI.Models;
using TimeTrackerAPI.Services;

namespace TimeTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("createUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == userDto.Login);
            if (existingUser != null)
            {
                return Conflict(new { message = "User with this login already exists." });
            }

            var user = new User
            {
                Login = userDto.Login,
                Email = userDto.Email,
                PasswordHash = SHA512Hasher.HashPassword(userDto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User created successfully.");
        }
    }
}
