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
    [Route("api/[controller]")]
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

            var roleResult = await _userManager.AddToRoleAsync(user, "User");

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

        [HttpPost("registerAdmin")]
        public async Task<IActionResult> RegisterAdmin(RegisterDto dto)
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

            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");

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
            var user = await _userManager.FindByEmailAsync(dto.Email);

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

        [Authorize(Roles = "User")]
        [HttpGet("testUser")]
        public IActionResult TestUser()
        {
            return Ok("usuario autorizado");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("testAdmin")]
        public IActionResult Test()
        {
            return Ok("Admin autorizado");
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

    }
}
