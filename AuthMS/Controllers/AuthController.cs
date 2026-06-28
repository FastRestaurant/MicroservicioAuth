using System.Security.Cryptography;
using Application.DTOs;
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
        private readonly JwtService _jwtService;
        private readonly AuthDbContext _context;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            JwtService jwtService,
            AuthDbContext authDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _context = authDbContext;
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 5;
            var query = _userManager.Users;
            var totalUsers = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            var users = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var result = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserResponseDto
                {
                    Id = user.Id,
                    Role = roles.FirstOrDefault() ?? "SIN ROL",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    Email = user.Email
                });
            }
            return Ok(new
            {
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                Data = result
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
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

            var roleResult = await _userManager.AddToRoleAsync(user,dto.Role);

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

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = _jwtService.GenerateRefreshToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };

            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                token,
                refreshToken = refreshToken.Token,
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
            var refreshToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.Token == dto.RefreshToken);

            if (refreshToken == null)
                return Unauthorized();

            if (refreshToken.RevokedAt != null)
                return Unauthorized();

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
                return Unauthorized();

            var newJwt =
                await _jwtService.CreateToken(refreshToken.User);

            refreshToken.RevokedAt = DateTime.UtcNow;

            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = _jwtService.GenerateRefreshToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = refreshToken.UserId
            };

            _context.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = newJwt,
                refreshToken = newRefreshToken.Token
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            // USERNAME
            if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
            {
                var usernameResult = await _userManager.SetUserNameAsync(user, dto.UserName);

                if (!usernameResult.Succeeded)
                    return BadRequest(new
                    {
                        message = "Error actualizando username",
                        errors = usernameResult.Errors
                    });
            }

            // EMAIL
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                var emailResult = await _userManager.SetEmailAsync(user, dto.Email);

                if (!emailResult.Succeeded)
                    return BadRequest(new
                    {
                        message = "Error actualizando email",
                        errors = emailResult.Errors
                    });
            }

            // NOMBRE Y APELLIDO (campos propios)
            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;

            // PASSWORD
            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);

                if (!removePasswordResult.Succeeded)
                    return BadRequest(new
                    {
                        message = "Error removiendo contraseña actual",
                        errors = removePasswordResult.Errors
                    });

                var addPasswordResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);

                if (!addPasswordResult.Succeeded)
                    return BadRequest(new
                    {
                        message = "Error estableciendo nueva contraseña",
                        errors = addPasswordResult.Errors
                    });
            }

            // ROLES
            if (!string.IsNullOrEmpty(dto.NewRol))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                // si no tiene el rol ya asignado
                if (!currentRoles.Contains(dto.NewRol))
                {
                    // remover roles actuales (si querés solo 1 rol por usuario)
                    foreach (var role in currentRoles)
                    {
                        var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, role);

                        if (!removeRoleResult.Succeeded)
                            return BadRequest(new
                            {
                                message = "Error removiendo rol actual",
                                errors = removeRoleResult.Errors
                            });
                    }

                    // agregar nuevo rol
                    var addRoleResult = await _userManager.AddToRoleAsync(user, dto.NewRol);

                    if (!addRoleResult.Succeeded)
                        return BadRequest(new
                        {
                            message = "Error asignando nuevo rol",
                            errors = addRoleResult.Errors
                        });
                }
            }

            return Ok(new { message = "Usuario actualizado correctamente" });
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Waitress")]
        [HttpGet("testWaitress")]
        public IActionResult TestUser()
        {
            return Ok("mecera autorizado");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("testAdmin")]
        public IActionResult TestAdmin()
        {
            return Ok("Admin autorizado");
        }

        [Authorize(Roles = "kitchen")]
        [HttpGet("testkitchen")]
        public IActionResult Testkitchen()
        {
            return Ok("cocinero autorizado");
        }

        [Authorize(Roles = "cashier")]
        [HttpGet("testcashier")]
        public IActionResult Test()
        {
            return Ok("cajero autorizado");
        }




    }
}
