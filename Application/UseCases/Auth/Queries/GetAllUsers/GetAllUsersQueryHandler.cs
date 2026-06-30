using Application.Common;
using Application.DTOs;
using Application.Interfaces;

namespace Application.UseCases.Auth.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler : IGetAllUsersQueryHandler
{
    private readonly IAuthService _authService;

    public GetAllUsersQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<UseCaseResult<UsersPageResponseDto>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken = default) =>
        _authService.GetAllUsers(query.PageNumber, query.PageSize, query.Search, query.Role, cancellationToken);
}
