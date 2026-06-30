namespace Application.UseCases.Auth.Queries.GetAllUsers;

public sealed class GetAllUsersQuery
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Search { get; init; }

    public string? Role { get; init; }
}
