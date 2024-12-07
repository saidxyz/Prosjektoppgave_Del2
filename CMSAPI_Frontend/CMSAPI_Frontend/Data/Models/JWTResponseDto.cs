namespace CMSAPI_Frontend.Data.Models
{
    public class JWTResponseDto
    {

        public Boolean IsSuccess { get; set; }
        public string Token { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
