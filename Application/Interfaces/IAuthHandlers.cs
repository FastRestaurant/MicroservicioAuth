using Application.Common;
using Application.DTOs;
using Application.UseCases.Auth.Commands.DeleteUser;
using Application.UseCases.Auth.Commands.Login;
using Application.UseCases.Auth.Commands.Logout;
using Application.UseCases.Auth.Commands.RefreshToken;
using Application.UseCases.Auth.Commands.RegisterUser;
using Application.UseCases.Auth.Commands.UpdateUser;
using Application.UseCases.Auth.Queries.GetAllRoles;
using Application.UseCases.Auth.Queries.GetAllUsers;
using Application.UseCases.Users.Queries.UserExists;

namespace Application.Interfaces;

public interface IGetAllRolesQueryHandler
{
    Task<UseCaseResult<IReadOnlyCollection<RoleResponseDto>>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken = default);
}

public interface IGetAllUsersQueryHandler
{
    Task<UseCaseResult<UsersPageResponseDto>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken = default);
}

public interface IRegisterUserCommandHandler
{
    Task<UseCaseResult<string>> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default);
}

public interface ILoginCommandHandler
{
    Task<UseCaseResult<LoginResponseDto>> Handle(LoginCommand command, CancellationToken cancellationToken = default);
}

public interface ILogoutCommandHandler
{
    Task<UseCaseResult<string>> Handle(LogoutCommand command, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenCommandHandler
{
    Task<UseCaseResult<RefreshResponseDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default);
}

public interface IUpdateUserCommandHandler
{
    Task<UseCaseResult<string>> Handle(UpdateUserCommand command, CancellationToken cancellationToken = default);
}

public interface IDeleteUserCommandHandler
{
    Task<UseCaseResult<string>> Handle(DeleteUserCommand command, CancellationToken cancellationToken = default);
}

public interface IUserExistsQueryHandler
{
    Task<bool> Handle(UserExistsQuery query, CancellationToken cancellationToken = default);
}
