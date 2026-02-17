using System.Collections.Generic;

namespace UP.Models
{
    public class UserData
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Token { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Allergies { get; set; } = new List<string>();
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserData Data { get; set; }
    }
}