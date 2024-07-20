using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeTrackerAPI.Controllers
{
    [Authorize]
    public class TimeController : ControllerBase
    {
        [HttpGet("secure-data")]
        [Authorize] // Allows access to users authenticated via either scheme
        public IActionResult GetSecureData()
        {
            // Your secured logic here
            return Ok(new { Data = "This is a secured data." });
        }
    }
}
