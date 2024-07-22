using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeTrackerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeController : ControllerBase
    {
        [HttpGet("secure-data")]
        [Authorize(Policy = "RequireAuthenticatedUser")]
        public IActionResult GetSecureData()
        {
            // Your secured logic here
            return Ok(new { Data = "This is a secured data." });
        }
    }
}
