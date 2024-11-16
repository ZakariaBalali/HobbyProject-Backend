using HobbyAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HobbyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Mock user data for testing (you can replace this with a database or other authentication logic)
        private static readonly string validUsername = "testuser";
        private static readonly string validPassword = "testpassword";  // In production, use hashed passwords!

        // POST /api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            if (login == null || string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
            {
                return BadRequest("Username and password are required.");
            }

            // Simple authentication logic (replace with actual logic)
            if (login.Username == validUsername && login.Password == validPassword)
            {
                // Authentication succeeded
                var response = new
                {
                    message = "Login successful",
                    username = login.Username
                };
                return Ok(response); // Return HTTP 200 with the message
            }

            // Authentication failed
            return Unauthorized(new { message = "Invalid username or password" }); // Return HTTP 401 for unauthorized
        }
    }
}
