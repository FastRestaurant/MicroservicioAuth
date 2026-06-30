using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Application.UseCases.Auth.Queries.GetAllRoles;

public sealed class GetAllRolesQueryHandler : IGetAllRolesQueryHandler
{
    private readonly IAuthService _authService;

    public GetAllRolesQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<IReadOnlyCollection<RoleResponseDto>>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken = default) =>
        _authService.GetAllRoles(cancellationToken);
}
