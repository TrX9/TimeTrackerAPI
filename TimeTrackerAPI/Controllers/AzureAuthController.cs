using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;

namespace TimeTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AzureAuthController : ControllerBase
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<AzureAuthController> _logger;

        public AzureAuthController(ITokenAcquisition tokenAcquisition, GraphServiceClient graphServiceClient, ILogger<AzureAuthController> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
            _logger = logger;
        }

        [HttpGet("login-azure")]
        [Authorize(Policy = "RequireAuthenticatedUser")]
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
    }
}
