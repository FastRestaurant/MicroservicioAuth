using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Application.UseCases.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRefreshTokenCommandHandler
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<RefreshResponseDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default) =>
        _authService.Refresh(command.Dto, cancellationToken);
}
