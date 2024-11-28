namespace clinic_system.DTOs.AuthDtos
{
    public class LoginResponseDto
    {
        public string Message { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
