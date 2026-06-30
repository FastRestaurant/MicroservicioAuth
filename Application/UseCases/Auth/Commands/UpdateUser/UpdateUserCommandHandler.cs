using Application.Common;
using Application.Interfaces;

namespace Application.UseCases.Auth.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : IUpdateUserCommandHandler
{
    private readonly IAuthService _authService;

    public UpdateUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<string>> Handle(UpdateUserCommand command, CancellationToken cancellationToken = default) =>
        _authService.UpdateUser(command.Id, command.Dto, cancellationToken);
}
