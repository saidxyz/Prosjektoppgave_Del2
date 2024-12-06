namespace Client.Data.Models;

public class JWTDto
{
    public Boolean IsSuccess { get; set; }
    
    public string Token { get; set; } = null!;
    
    public string Message { get; set; } = null!;
}