using Application.Common;
using Application.Interfaces;

namespace Application.UseCases.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRegisterUserCommandHandler
{
    private readonly IAuthService _authService;

    public RegisterUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<string>> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default) =>
        _authService.Register(command.Dto, cancellationToken);
}
