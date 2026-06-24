using System.IO;

namespace StickItApp.Services;

public static class ImagePathService
{
    public static string ToImageSourcePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string trimmed = path.Trim();
        if (trimmed.StartsWith("pack://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return trimmed.Replace('\\', '/');
        }

        if (Path.IsPathRooted(trimmed))
        {
            return File.Exists(trimmed) ? trimmed : string.Empty;
        }

        string outputPath = Path.Combine(AppContext.BaseDirectory, trimmed);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        string normalized = trimmed.Replace('\\', '/');
        return normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? "/" + normalized
            : string.Empty;
    }
}
