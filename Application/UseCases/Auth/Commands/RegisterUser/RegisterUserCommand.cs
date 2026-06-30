using Application.DTOs;

namespace Application.UseCases.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommand
{
    public RegisterDto Dto { get; init; } = null!;
}
