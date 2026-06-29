namespace Domain.Constants;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Waitress = "Waitress";
    public const string Kitchen = "Kitchen";
    public const string Cashier = "Cashier";
    public const string AllCsv = $"{Admin},{Waitress},{Kitchen},{Cashier}";

    public static readonly string[] All =
    {
        Admin,
        Waitress,
        Kitchen,
        Cashier
    };

    public static bool TryNormalize(string? role, out string normalizedRole)
    {
        normalizedRole = All.FirstOrDefault(value =>
            string.Equals(value, role, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        return normalizedRole.Length > 0;
    }
}
