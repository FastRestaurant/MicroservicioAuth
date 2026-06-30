using Application.DTOs;

namespace Application.UseCases.Auth.Commands.Login;

public sealed class LoginCommand
{
    public LoginDto Dto { get; init; } = null!;
}
