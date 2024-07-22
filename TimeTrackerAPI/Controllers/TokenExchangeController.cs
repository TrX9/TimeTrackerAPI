using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace TimeTrackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenExchangeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenExchangeController> _logger;

        public TokenExchangeController(IConfiguration configuration, ILogger<TokenExchangeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("exchange-token")]
        public async Task<IActionResult> ExchangeToken([FromBody] TokenExchangeRequest request)
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var redirectUri = _configuration["AzureAd:RedirectUri"];
            var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            var httpClient = new HttpClient();
            var parameters = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", request.Code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
            });

            var response = await httpClient.PostAsync(tokenEndpoint, parameters);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error fetching token from Azure AD: {0}", responseContent);
                return StatusCode((int)response.StatusCode, responseContent);
            }

            var tokenResponse = JObject.Parse(responseContent);
            var accessToken = tokenResponse["access_token"]?.ToString();

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Token not found in response: {0}", responseContent);
                return BadRequest("Token not found in response");
            }

            // Check if the user exists in the Azure AD directory
            bool userExists = await CheckIfUserExistsInDirectory(accessToken);

            if (!userExists)
            {
                return Unauthorized("User is not present in the directory.");
            }

            return Ok(new { AccessToken = accessToken });
        }

        private async Task<bool> CheckIfUserExistsInDirectory(string accessToken)
        {
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists in directory.");
                return false;
            }
        }

        public class TokenExchangeRequest
        {
            public string Code { get; set; }
        }
    }
}
