using Application.Common;
using Application.Interfaces;

namespace Application.UseCases.Auth.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : IDeleteUserCommandHandler
{
    private readonly IAuthService _authService;

    public DeleteUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<string>> Handle(DeleteUserCommand command, CancellationToken cancellationToken = default) =>
        _authService.DeleteUser(command.Id, cancellationToken);
}
