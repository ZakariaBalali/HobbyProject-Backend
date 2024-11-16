namespace HobbyProject_Backend.Models
{
    // User model to represent user data in both register and login controllers
    public class UserModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // Login model to handle login request data
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // Register model to handle registration request data
    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
