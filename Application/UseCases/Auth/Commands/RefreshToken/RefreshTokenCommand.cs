using Application.DTOs;

namespace Application.UseCases.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommand
{
    public RefreshTokenDto Dto { get; init; } = null!;
}
