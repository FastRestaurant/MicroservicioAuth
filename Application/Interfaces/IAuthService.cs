using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<UseCaseResult<IReadOnlyCollection<RoleResponseDto>>> GetAllRoles(CancellationToken cancellationToken = default);

    Task<UseCaseResult<UsersPageResponseDto>> GetAllUsers(int pageNumber, int pageSize, string? search, string? role, CancellationToken cancellationToken = default);

    Task<UseCaseResult<string>> Register(RegisterDto dto, CancellationToken cancellationToken = default);

    Task<UseCaseResult<LoginResponseDto>> Login(LoginDto dto, CancellationToken cancellationToken = default);

    Task<UseCaseResult<string>> Logout(string? userId, CancellationToken cancellationToken = default);

    Task<UseCaseResult<RefreshResponseDto>> Refresh(RefreshTokenDto dto, CancellationToken cancellationToken = default);

    Task<UseCaseResult<string>> UpdateUser(string id, UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<UseCaseResult<string>> DeleteUser(string id, CancellationToken cancellationToken = default);

    Task<bool> UserExists(string id, CancellationToken cancellationToken = default);
}
