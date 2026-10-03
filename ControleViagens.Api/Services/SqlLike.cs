namespace ControleViagens.Api.Services;

/// <summary>Builds ILIKE patterns that treat the user's text literally.</summary>
internal static class SqlLike
{
    public static string Contains(string value) => $"%{Escape(value)}%";

    public static string StartsWith(string value) => $"{Escape(value)}%";

    private static string Escape(string value) =>
        value.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
