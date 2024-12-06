using System.ComponentModel.DataAnnotations;
namespace Client.Data.Models;

public class LoginDto
{
    [Required(ErrorMessage = "Email required!")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Password required!")]
    public string Password { get; set; } = null!;
}