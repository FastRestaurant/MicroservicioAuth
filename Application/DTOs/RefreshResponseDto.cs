namespace Application.DTOs;

public sealed class RefreshResponseDto
{
    public string Token { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;
}
