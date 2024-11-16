using HobbyProject_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text.Json;
using BCrypt.Net;
namespace HobbyProject_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            if (login == null || string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                var existingUsers = LoadUsersFromFile();
                var user = existingUsers.FirstOrDefault(u => u.Username == login.Username);
                if (user != null && BCrypt.Net.BCrypt.Verify(login.Password, user.Password))  // Verify the hashed password
                {
                    var token = JwtHelper.GenerateToken(login.Username);

                    var response = new
                    {
                        message = "Login successful",
                        username = login.Username,
                        token = token
                    };

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error while processing the request: " + ex.Message);
            }

            return Unauthorized(new { message = "Invalid username or password" });
        }

        private List<UserModel> LoadUsersFromFile()
        {
            if (System.IO.File.Exists(usersFilePath))
            {
                var jsonData = System.IO.File.ReadAllText(usersFilePath);
                return JsonSerializer.Deserialize<List<UserModel>>(jsonData) ?? new List<UserModel>();
            }
            return new List<UserModel>();
        }
    }

    // Register Controller to handle user registration
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private static readonly string usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel register)
        {
            if (register == null || string.IsNullOrEmpty(register.Username) || string.IsNullOrEmpty(register.Password))
            {
                return BadRequest("Username and password are required.");
            }

            try
            {
                var existingUsers = LoadUsersFromFile();
                if (existingUsers.Any(u => u.Username == register.Username))
                {
                    return BadRequest("Username already exists.");
                }

                // Hash the password before saving
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(register.Password);

                var newUser = new UserModel
                {
                    Username = register.Username,
                    Password = hashedPassword  // Store the hashed password
                };

                existingUsers.Add(newUser);

                var jsonData = JsonSerializer.Serialize(existingUsers, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(usersFilePath, jsonData);

                return Ok(new { message = "Registration successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error while processing the request: " + ex.Message);
            }
        }

        private List<UserModel> LoadUsersFromFile()
        {
            if (System.IO.File.Exists(usersFilePath))
            {
                var jsonData = System.IO.File.ReadAllText(usersFilePath);
                return JsonSerializer.Deserialize<List<UserModel>>(jsonData) ?? new List<UserModel>();
            }
            return new List<UserModel>();
        }
    }

}
