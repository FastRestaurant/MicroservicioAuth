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
        [ProducesResponseType(typeof(IReadOnlyCollection<RoleResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
        {
            var result = await _getAllRolesHandler.Handle(new GetAllRolesQuery(), cancellationToken);

            return ToActionResult(result);
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("users")]
        [ProducesResponseType(typeof(UsersPageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
        {
            var result = await _registerUserHandler.Handle(new RegisterUserCommand
            {
                Dto = dto
            }, cancellationToken);

            return result.Status == UseCaseStatus.Ok
                ? Created(string.Empty, result.Data)
                : ToActionResult(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var result = await _logoutHandler.Handle(new LogoutCommand
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            }, cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
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
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
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
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
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
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult TestUser()
        {
            return Ok("mecera autorizado");
        }

        [Authorize(Roles = ApplicationRoles.Admin)]
        [HttpGet("testAdmin")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult TestAdmin()
        {
            return Ok("Admin autorizado");
        }

        [Authorize(Roles = ApplicationRoles.Kitchen)]
        [HttpGet("testkitchen")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Testkitchen()
        {
            return Ok("cocinero autorizado");
        }

        [Authorize(Roles = ApplicationRoles.Cashier)]
        [HttpGet("testcashier")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Test()
        {
            return Ok("cajero autorizado");
        }

        private IActionResult ToActionResult<T>(UseCaseResult<T> result)
        {
            return result.Status switch
            {
                UseCaseStatus.Ok => ToOkResult(result),
                UseCaseStatus.BadRequest => BadRequest(ToErrorPayload(result, StatusCodes.Status400BadRequest)),
                UseCaseStatus.Conflict => Conflict(ToErrorPayload(result, StatusCodes.Status409Conflict)),
                UseCaseStatus.Unauthorized => Unauthorized(ToErrorPayload(result, StatusCodes.Status401Unauthorized, "No autorizado.")),
                UseCaseStatus.NotFound => NotFound(ToErrorPayload(result, StatusCodes.Status404NotFound)),
                _ => BadRequest(ToErrorPayload(result, StatusCodes.Status400BadRequest))
            };
        }

        private IActionResult ToOkResult<T>(UseCaseResult<T> result)
        {
            if (result.Data is string value && string.IsNullOrEmpty(value))
                return Ok();

            return Ok(result.Data);
        }

        private static ErrorResponseDto ToErrorPayload<T>(UseCaseResult<T> result, int statusCode, string defaultMessage = "La solicitud no es válida.")
        {
            return new ErrorResponseDto
            {
                Message = string.IsNullOrWhiteSpace(result.Message) ? defaultMessage : result.Message,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow,
                Errors = result.Errors
            };
        }
    }
}
