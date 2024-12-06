using System.ComponentModel.DataAnnotations;

namespace Client.Data.Models
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username required!")]
        public string Username { get; set; } = null!;
        
        [Required(ErrorMessage = "Email required!")]
        public string Email { get; set; } = null!;
        
        [Required(ErrorMessage = "Password required!")]
        public string Password { get; set; } = null!;
        
    }
}