using System.Security.Claims;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases.Auth.Commands.DeleteUser;
using Application.UseCases.Auth.Commands.Login;
using Application.UseCases.Auth.Commands.Logout;
using Application.UseCases.Auth.Commands.RefreshToken;
using Application.UseCases.Auth.Commands.RegisterUser;
using Application.UseCases.Auth.Commands.UpdateUser;
using Application.UseCases.Auth.Queries.GetAllRoles;
using Application.UseCases.Auth.Queries.GetAllUsers;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IGetAllRolesQueryHandler _getAllRolesHandler;
        private readonly IGetAllUsersQueryHandler _getAllUsersHandler;
        private readonly IRegisterUserCommandHandler _registerUserHandler;
        private readonly ILoginCommandHandler _loginHandler;
        private readonly ILogoutCommandHandler _logoutHandler;
        private readonly IRefreshTokenCommandHandler _refreshTokenHandler;
        private readonly IUpdateUserCommandHandler _updateUserHandler;
        private readonly IDeleteUserCommandHandler _deleteUserHandler;

        public AuthController(
            IGetAllRolesQueryHandler getAllRolesHandler,
            IGetAllUsersQueryHandler getAllUsersHandler,
            IRegisterUserCommandHandler registerUserHandler,
            ILoginCommandHandler loginHandler,
            ILogoutCommandHandler logoutHandler,
            IRefreshTokenCommandHandler refreshTokenHandler,
            IUpdateUserCommandHandler updateUserHandler,
            IDeleteUserCommandHandler deleteUserHandler)
        {
            _getAllRolesHandler = getAllRolesHandler;
            _getAllUsersHandler = getAllUsersHandler;
            _registerUserHandler = registerUserHandler;
            _loginHandler = loginHandler;
            _logoutHandler = logoutHandler;
            _refreshTokenHandler = refreshTokenHandler;
            _updateUserHandler = updateUserHandler;
            _deleteUserHandler = deleteUserHandler;
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
        {
            var result = await _getAllRolesHandler.Handle(new GetAllRolesQuery(), cancellationToken);

            return ToActionResult(result);
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(int pageNumber = 1, int pageSize = 10, string? search = null, string? role = null, CancellationToken cancellationToken = default)
        {
            var result = await _getAllUsersHandler.Handle(new GetAllUsersQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                Role = role
            }, cancellationToken);

            return ToActionResult(result);
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
        {
            var result = await _registerUserHandler.Handle(new RegisterUserCommand
            {
                Dto = dto
            }, cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto, CancellationToken cancellationToken)
        {
            var result = await _loginHandler.Handle(new LoginCommand
            {
                Dto = dto
            }, cancellationToken);

            return ToActionResult(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var result = await _logoutHandler.Handle(new LogoutCommand
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            }, cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenDto dto, CancellationToken cancellationToken)
        {
            var result = await _refreshTokenHandler.Handle(new RefreshTokenCommand
            {
                Dto = dto
            }, cancellationToken);

            return ToActionResult(result);
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpPatch("user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto dto, CancellationToken cancellationToken)
        {
            var result = await _updateUserHandler.Handle(new UpdateUserCommand
            {
                Id = id,
                Dto = dto
            }, cancellationToken);

            return ToActionResult(result);
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
        {
            var result = await _deleteUserHandler.Handle(new DeleteUserCommand
            {
                Id = id
            }, cancellationToken);

            return ToActionResult(result);
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

        private IActionResult ToActionResult<T>(UseCaseResult<T> result)
        {
            return result.Status switch
            {
                UseCaseStatus.Ok => ToOkResult(result),
                UseCaseStatus.BadRequest => BadRequest(ToErrorPayload(result)),
                UseCaseStatus.Unauthorized => string.IsNullOrWhiteSpace(result.Message) ? Unauthorized() : Unauthorized(result.Message),
                UseCaseStatus.NotFound => NotFound(new { message = result.Message }),
                _ => BadRequest()
            };
        }

        private IActionResult ToOkResult<T>(UseCaseResult<T> result)
        {
            if (result.Data is string value && string.IsNullOrEmpty(value))
                return Ok();

            return Ok(result.Data);
        }

        private static object ToErrorPayload<T>(UseCaseResult<T> result)
        {
            if (result.Errors.Count > 0 && string.IsNullOrWhiteSpace(result.Message))
                return result.Errors;

            if (result.Errors.Count > 0)
                return new { message = result.Message, errors = result.Errors };

            return new { message = result.Message };
        }
    }
}
