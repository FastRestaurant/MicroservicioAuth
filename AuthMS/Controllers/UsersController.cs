using Application.Common;
using Application.Interfaces;
using Application.UseCases.Users.Queries.UserExists;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = ApplicationRoles.AllCsv)]
public class UsersController : ControllerBase
{
    private readonly IUserExistsQueryHandler _userExistsHandler;

    public UsersController(IUserExistsQueryHandler userExistsHandler)
    {
        _userExistsHandler = userExistsHandler;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exists(string id, CancellationToken cancellationToken)
    {
        var exists = await _userExistsHandler.Handle(new UserExistsQuery
        {
            Id = id
        }, cancellationToken);

        if (!exists)
            return NotFound(new ErrorResponseDto
            {
                Message = "Usuario no encontrado",
                StatusCode = StatusCodes.Status404NotFound,
                Timestamp = DateTime.UtcNow
            });

        return NoContent();
    }
}
