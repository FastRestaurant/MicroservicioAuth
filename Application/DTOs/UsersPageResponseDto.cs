namespace Application.DTOs;

public sealed class UsersPageResponseDto
{
    public int CurrentPage { get; init; }

    public int Page { get; init; }

    public int TotalPages { get; init; }

    public int PageSize { get; init; }

    public int TotalUsers { get; init; }

    public int TotalItems { get; init; }

    public IReadOnlyDictionary<string, int> RoleCounts { get; init; } = new Dictionary<string, int>();

    public IReadOnlyCollection<UserResponseDto> Data { get; init; } = Array.Empty<UserResponseDto>();
}
