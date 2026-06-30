using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Application.UseCases.Auth.Commands.Login;

public sealed class LoginCommandHandler : ILoginCommandHandler
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<LoginResponseDto>> Handle(LoginCommand command, CancellationToken cancellationToken = default) =>
        _authService.Login(command.Dto, cancellationToken);
}
