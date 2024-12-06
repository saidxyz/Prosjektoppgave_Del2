namespace Client.Data.Models;

public class TokenDto
{
    public bool IsSuccess { get; set; }
    public string Token { get; set; }
    public string Message { get; set; }
}