using Application.Interfaces;

namespace Application.UseCases.Users.Queries.UserExists;

public sealed class UserExistsQueryHandler : IUserExistsQueryHandler
{
    private readonly IAuthService _authService;

    public UserExistsQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<bool> Handle(UserExistsQuery query, CancellationToken cancellationToken = default) =>
        _authService.UserExists(query.Id, cancellationToken);
}
