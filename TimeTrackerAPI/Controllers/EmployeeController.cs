using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        [HttpGet]
        [Authorize(AuthenticationSchemes = "Local,AzureAd")]
        public IActionResult GetEmployees()
        {
            // Your logic to get employees
            return Ok(new { Message = "Access granted for both local and Azure AD authenticated users." });
        }
    }
}
