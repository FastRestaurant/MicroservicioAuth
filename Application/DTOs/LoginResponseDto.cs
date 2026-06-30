namespace Application.DTOs;

public sealed class LoginResponseDto
{
    public string Token { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? UserName { get; init; }
}
