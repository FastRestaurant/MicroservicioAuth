using Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace API.Middlewares;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "El registro fue modificado por otro usuario. Recargue e intente nuevamente.");
        }
        catch (DbUpdateException exception) when (IsExpectedDataConflict(exception))
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "La operación entra en conflicto con datos existentes. Recargue e intente nuevamente.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ocurrio una excepcion no controlada mientras se procesaba la solicitud.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Ocurrio un error inesperado.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Message = message,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow
        });
    }

    private static bool IsExpectedDataConflict(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == 2601 || sqlException.Number == 2627 || sqlException.Number == 547);
    }
}
