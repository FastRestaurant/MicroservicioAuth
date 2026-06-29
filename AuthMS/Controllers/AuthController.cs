using Application.DTOs;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;
        private readonly AuthDbContext _context;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtService jwtService,
            AuthDbContext authDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _context = authDbContext;
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _context.Roles
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    NormalizedName = r.NormalizedName
                }).ToListAsync();
            return Ok(roles);
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 10, string? search = null, string? role = null)
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
                .ToDictionaryAsync(item => item.Role, item => item.Count);

            var query = searchQuery;

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (!ApplicationRoles.TryNormalize(role, out var normalizedRole))
                    return BadRequest(new { message = "Rol invalido" });

                var usersInRole =
                    from userRole in _context.UserRoles.AsNoTracking()
                    join identityRole in _context.Roles.AsNoTracking() on userRole.RoleId equals identityRole.Id
                    where identityRole.Name == normalizedRole
                    select userRole.UserId;

                query = query.Where(user => usersInRole.Contains(user.Id));
            }

            var totalUsers = await query.CountAsync();
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
                .ToListAsync();

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
                .ToListAsync();

            var rolesByUserId = userRoles
                .GroupBy(item => item.UserId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Role).FirstOrDefault());

            var result = users.Select(user => new UserResponseDto
            {
                Id = user.Id,
                Role = rolesByUserId.GetValueOrDefault(user.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email
            }).ToList();

            return Ok(new
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

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ApplicationRoles.TryNormalize(dto.Role, out var role))
                return BadRequest(new { message = "Rol inválido" });

            if (!await _roleManager.RoleExistsAsync(role))
                return BadRequest(new { message = "Rol inválido" });

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
                return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {

                await _userManager.DeleteAsync(user);

                return BadRequest(new
                {
                    message = "Error asignando rol, usuario eliminado",
                    errors = roleResult.Errors
                });
            }

            return Ok("Usuario creado correctamente");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);

            if (user == null)
                return Unauthorized("Usuario no encontrado");

            var result = await _userManager.CheckPasswordAsync(
                user,
                dto.Password);

            if (!result)
                return Unauthorized("Credenciales inválidas");

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

            await _context.SaveChangesAsync();

            return Ok(new
            {
                token,
                refreshToken = refreshTokenValue,
                email = user.Email,
                userName = user.UserName
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var tokens = await _context.RefreshTokens
                .Where(t =>
                    t.UserId == user.Id &&
                    t.RevokedAt == null)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return Unauthorized();

            var tokenHash = _jwtService.HashRefreshToken(dto.RefreshToken);

            var refreshToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.TokenHash == tokenHash);

            if (refreshToken == null)
                return Unauthorized();

            if (refreshToken.RevokedAt != null)
                return Unauthorized();

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized();

            var newJwt =
                await _jwtService.CreateToken(refreshToken.User);
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

            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = newJwt,
                refreshToken = newRefreshTokenValue
            });
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpPatch("user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
            {
                var usernameResult = await _userManager.SetUserNameAsync(user, dto.UserName);
                if (!usernameResult.Succeeded)
                    return BadRequest(new { message = "Error actualizando username", errors = usernameResult.Errors });
            }

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, dto.Email);
                if (!emailResult.Succeeded)
                    return BadRequest(new { message = "Error actualizando email", errors = emailResult.Errors });
            }

            if (!string.IsNullOrEmpty(dto.FirstName))
            {
                user.FirstName = dto.FirstName;
            }

            if (!string.IsNullOrEmpty(dto.LastName))
            {
                user.LastName = dto.LastName;
            }

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (!ApplicationRoles.TryNormalize(dto.Role, out var role))
                    return BadRequest(new { message = "Rol inválido" });

                if (!await _roleManager.RoleExistsAsync(role))
                    return BadRequest(new { message = "Rol inválido" });

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Any(currentRole => string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase)))
                {
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeRolesResult.Succeeded)
                        return BadRequest(new { message = "Error removiendo rol actual", errors = removeRolesResult.Errors });

                    var addRoleResult = await _userManager.AddToRoleAsync(user, role);
                    if (!addRoleResult.Succeeded)
                        return BadRequest(new { message = "Error asignando rol", errors = addRoleResult.Errors });
                }
            }

            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                var passwordResult = await _userManager.RemovePasswordAsync(user);
                if (!passwordResult.Succeeded)
                    return BadRequest(new { message = "Error removiendo contraseña actual", errors = passwordResult.Errors });

                var addPasswordResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
                if (!addPasswordResult.Succeeded)
                    return BadRequest(new { message = "Error estableciendo nueva contraseña", errors = addPasswordResult.Errors });
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(new { message = "Error guardando cambios", errors = updateResult.Errors });

            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var userDelete = await _userManager.FindByIdAsync(id);

            if (userDelete == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var admin = await _userManager.FindByNameAsync("admin");
            var currentUserId = admin.Id;

            if (userDelete.Id == currentUserId)
                return BadRequest(new { message = "No puedes eliminar tu propia cuenta" });

            var result = await _userManager.DeleteAsync(userDelete);

            if (!result.Succeeded)
                return BadRequest(new { message = "Error eliminando usuario", errors = result.Errors });

            return Ok(new { message = "Usuario eliminado correctamente" });
        }

        [Authorize(Roles = ApplicationRoles.Waitress)]
        [HttpGet("testWaitress")]
        public IActionResult TestUser()
        {
            return Ok("mecera autorizado");
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("testAdmin")]
        public IActionResult TestAdmin()
        {
            return Ok("Admin autorizado");
        }

        [Authorize(Roles = ApplicationRoles.Kitchen)]
        [HttpGet("testkitchen")]
        public IActionResult Testkitchen()
        {
            return Ok("cocinero autorizado");
        }

        [Authorize(Roles = ApplicationRoles.Cashier)]
        [HttpGet("testcashier")]
        public IActionResult Test()
        {
            return Ok("cajero autorizado");
        }
    }
}
