using Application.Common;
using Application.Interfaces;

namespace Application.UseCases.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : ILogoutCommandHandler
{
    private readonly IAuthService _authService;

    public LogoutCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<string>> Handle(LogoutCommand command, CancellationToken cancellationToken = default) =>
        _authService.Logout(command.UserId, cancellationToken);
}
