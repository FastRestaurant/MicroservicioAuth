using Application.DTOs;

namespace Application.UseCases.Auth.Commands.UpdateUser;

public sealed class UpdateUserCommand
{
    public string Id { get; init; } = string.Empty;

    public UpdateUserDto Dto { get; init; } = null!;
}
