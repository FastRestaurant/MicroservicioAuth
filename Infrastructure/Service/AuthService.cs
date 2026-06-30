using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly AuthDbContext _context;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        AuthDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _context = context;
    }

    public async Task<UseCaseResult<IReadOnlyCollection<RoleResponseDto>>> GetAllRoles(CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .Select(role => new RoleResponseDto
            {
                Id = role.Id,
                NormalizedName = role.NormalizedName ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return UseCaseResult<IReadOnlyCollection<RoleResponseDto>>.Ok(roles);
    }

    public async Task<UseCaseResult<UsersPageResponseDto>> GetAllUsers(int pageNumber, int pageSize, string? search, string? role, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var searchQuery = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            searchQuery = searchQuery.Where(user =>
                EF.Functions.Like(user.FirstName, term) ||
                EF.Functions.Like(user.LastName, term) ||
                EF.Functions.Like(user.UserName!, term) ||
                EF.Functions.Like(user.Email!, term));
        }

        var roleCounts = await (
            from user in searchQuery
            join userRole in _context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join identityRole in _context.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id
            group user by identityRole.Name into grouped
            select new
            {
                Role = grouped.Key ?? string.Empty,
                Count = grouped.Count()
            })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        var query = searchQuery;

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!ApplicationRoles.TryNormalize(role, out var normalizedRole))
                return UseCaseResult<UsersPageResponseDto>.BadRequest("Rol invalido");

            var usersInRole =
                from userRole in _context.UserRoles.AsNoTracking()
                join identityRole in _context.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id
                where identityRole.Name == normalizedRole
                select userRole.UserId;

            query = query.Where(user => usersInRole.Contains(user.Id));
        }

        var totalUsers = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
        if (totalPages > 0 && pageNumber > totalPages) pageNumber = totalPages;

        var users = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.UserName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.UserName,
                user.Email
            })
            .ToListAsync(cancellationToken);

        var pageUserIds = users.Select(user => user.Id).ToArray();
        var userRoles = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join identityRole in _context.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id
            where pageUserIds.Contains(userRole.UserId)
            select new
            {
                userRole.UserId,
                Role = identityRole.Name
            })
            .ToListAsync(cancellationToken);

        var rolesByUserId = userRoles
            .GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Role).FirstOrDefault());

        var result = users.Select(user => new UserResponseDto
        {
            Id = user.Id,
            Role = rolesByUserId.GetValueOrDefault(user.Id) ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty
        }).ToList();

        return UseCaseResult<UsersPageResponseDto>.Ok(new UsersPageResponseDto
        {
            CurrentPage = pageNumber,
            Page = pageNumber,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalUsers = totalUsers,
            TotalItems = totalUsers,
            RoleCounts = roleCounts,
            Data = result
        });
    }

    public async Task<UseCaseResult<string>> Register(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (!ApplicationRoles.TryNormalize(dto.Role, out var role))
            return UseCaseResult<string>.BadRequest("Rol inválido");

        if (!await _roleManager.RoleExistsAsync(role))
            return UseCaseResult<string>.BadRequest("Rol inválido");

        var user = new AppUser
        {
            UserName = dto.UserName,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return UseCaseResult<string>.BadRequest(string.Empty, ToErrors(result.Errors));

        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return UseCaseResult<string>.BadRequest("Error asignando rol, usuario eliminado", ToErrors(roleResult.Errors));
        }

        return UseCaseResult<string>.Ok("Usuario creado correctamente");
    }

    public async Task<UseCaseResult<LoginResponseDto>> Login(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName);

        if (user == null)
            return UseCaseResult<LoginResponseDto>.Unauthorized("Usuario no encontrado");

        var result = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
            return UseCaseResult<LoginResponseDto>.Unauthorized("Credenciales inválidas");

        var token = await _jwtService.CreateToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _jwtService.HashRefreshToken(refreshTokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        return UseCaseResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshTokenValue,
            Email = user.Email,
            UserName = user.UserName
        });
    }

    public async Task<UseCaseResult<string>> Logout(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return UseCaseResult<string>.Unauthorized();

        var tokens = await _context.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return UseCaseResult<string>.Ok(string.Empty);
    }

    public async Task<UseCaseResult<RefreshResponseDto>> Refresh(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return UseCaseResult<RefreshResponseDto>.Unauthorized();

        var tokenHash = _jwtService.HashRefreshToken(dto.RefreshToken);

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (refreshToken == null)
            return UseCaseResult<RefreshResponseDto>.Unauthorized();

        if (refreshToken.RevokedAt != null)
            return UseCaseResult<RefreshResponseDto>.Unauthorized();

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            return UseCaseResult<RefreshResponseDto>.Unauthorized();

        var user = await _userManager.FindByIdAsync(refreshToken.UserId);

        if (user == null)
            return UseCaseResult<RefreshResponseDto>.Unauthorized();

        var newJwt = await _jwtService.CreateToken(user);
        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();

        refreshToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _jwtService.HashRefreshToken(newRefreshTokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = refreshToken.UserId
        };

        _context.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        return UseCaseResult<RefreshResponseDto>.Ok(new RefreshResponseDto
        {
            Token = newJwt,
            RefreshToken = newRefreshTokenValue
        });
    }

    public async Task<UseCaseResult<string>> UpdateUser(string id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return UseCaseResult<string>.NotFound("Usuario no encontrado");

        if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
        {
            var usernameResult = await _userManager.SetUserNameAsync(user, dto.UserName);
            if (!usernameResult.Succeeded)
                return UseCaseResult<string>.BadRequest("Error actualizando username", ToErrors(usernameResult.Errors));
        }

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            var emailResult = await _userManager.SetEmailAsync(user, dto.Email);
            if (!emailResult.Succeeded)
                return UseCaseResult<string>.BadRequest("Error actualizando email", ToErrors(emailResult.Errors));
        }

        if (!string.IsNullOrEmpty(dto.FirstName))
            user.FirstName = dto.FirstName;

        if (!string.IsNullOrEmpty(dto.LastName))
            user.LastName = dto.LastName;

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (!ApplicationRoles.TryNormalize(dto.Role, out var role))
                return UseCaseResult<string>.BadRequest("Rol inválido");

            if (!await _roleManager.RoleExistsAsync(role))
                return UseCaseResult<string>.BadRequest("Rol inválido");

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Any(currentRole => string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase)))
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRolesResult.Succeeded)
                    return UseCaseResult<string>.BadRequest("Error removiendo rol actual", ToErrors(removeRolesResult.Errors));

                var addRoleResult = await _userManager.AddToRoleAsync(user, role);
                if (!addRoleResult.Succeeded)
                    return UseCaseResult<string>.BadRequest("Error asignando rol", ToErrors(addRoleResult.Errors));
            }
        }

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            var passwordResult = await _userManager.RemovePasswordAsync(user);
            if (!passwordResult.Succeeded)
                return UseCaseResult<string>.BadRequest("Error removiendo contraseña actual", ToErrors(passwordResult.Errors));

            var addPasswordResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!addPasswordResult.Succeeded)
                return UseCaseResult<string>.BadRequest("Error estableciendo nueva contraseña", ToErrors(addPasswordResult.Errors));
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return UseCaseResult<string>.BadRequest("Error guardando cambios", ToErrors(updateResult.Errors));

        return UseCaseResult<string>.Ok("Usuario actualizado correctamente");
    }

    public async Task<UseCaseResult<string>> DeleteUser(string id, CancellationToken cancellationToken = default)
    {
        var userDelete = await _userManager.FindByIdAsync(id);

        if (userDelete == null)
            return UseCaseResult<string>.NotFound("Usuario no encontrado");

        var admin = await _userManager.FindByNameAsync("admin");

        if (admin != null && userDelete.Id == admin.Id)
            return UseCaseResult<string>.BadRequest("No puedes eliminar tu propia cuenta");

        var result = await _userManager.DeleteAsync(userDelete);

        if (!result.Succeeded)
            return UseCaseResult<string>.BadRequest("Error eliminando usuario", ToErrors(result.Errors));

        return UseCaseResult<string>.Ok("Usuario eliminado correctamente");
    }

    public async Task<bool> UserExists(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);

        return user is not null;
    }

    private static IReadOnlyCollection<UseCaseError> ToErrors(IEnumerable<IdentityError> errors) =>
        errors.Select(error => new UseCaseError
        {
            Code = error.Code,
            Description = error.Description
        }).ToArray();
}
