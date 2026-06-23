using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Validates current JWT token to confirm it's still valid.
        /// Requires authorization (valid token).
        /// </summary>
        [Authorize]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return Ok(new
            {
                status = "ValidToken",
                message = "Login Successfull"
            });
        }

        /// <summary>
        /// Validates current JWT token before logout.
        /// Requires authorization (valid token).
        /// </summary>
        [Authorize]
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            try
            {
                return Ok(new
                {
                    status = "ValidToken",
                    message = "Logout Successfull"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, msg = ex.Message });
            }
        }
    }
}
