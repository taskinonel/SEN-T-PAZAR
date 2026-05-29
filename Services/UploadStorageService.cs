namespace SEN_T_PAZAR.Services;

public interface IUploadStorageService
{
    string RootPath { get; }
    string RequestPath { get; }
    string EnsureDirectory(params string[] segments);
    string GetPublicDirectory(params string[] segments);
    string? TryGetPhysicalPath(string? publicPath);
}

public sealed class UploadStorageService : IUploadStorageService
{
    private readonly StringComparison _pathComparison;

    public UploadStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        RootPath = Path.GetFullPath(ResolveRootPath(environment.ContentRootPath, configuration["Uploads:RootPath"]));
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string RequestPath => "/uploads";

    public string EnsureDirectory(params string[] segments)
    {
        var fullPath = CombineWithinRoot(segments);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public string GetPublicDirectory(params string[] segments)
    {
        var normalizedSegments = NormalizeSegments(segments);
        if (normalizedSegments.Count == 0)
        {
            return RequestPath;
        }

        return RequestPath + "/" + string.Join('/', normalizedSegments);
    }

    public string? TryGetPhysicalPath(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath))
        {
            return null;
        }

        if (Uri.TryCreate(publicPath, UriKind.Absolute, out _))
        {
            return null;
        }

        var normalizedPath = publicPath.Split('?', '#')[0].Trim();
        if (!normalizedPath.StartsWith(RequestPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var remainder = normalizedPath[RequestPath.Length..].TrimStart('/', '\\');
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return RootPath;
        }

        var segments = remainder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return CombineWithinRoot(segments);
    }

    private static string ResolveRootPath(string contentRootPath, string? configuredRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return Path.IsPathRooted(configuredRootPath)
                ? configuredRootPath
                : Path.GetFullPath(Path.Combine(contentRootPath, configuredRootPath));
        }

        var parentDirectory = Directory.GetParent(contentRootPath)?.FullName;
        var baseDirectory = string.IsNullOrWhiteSpace(parentDirectory)
            ? contentRootPath
            : parentDirectory;

        return Path.Combine(baseDirectory, "uploads");
    }

    private string CombineWithinRoot(IEnumerable<string> segments)
    {
        var normalizedSegments = NormalizeSegments(segments);
        var combinedPath = normalizedSegments.Count == 0
            ? RootPath
            : Path.Combine(new[] { RootPath }.Concat(normalizedSegments).ToArray());
        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(RootPath, _pathComparison))
        {
            throw new InvalidOperationException("Uploads path cannot escape the configured root directory.");
        }

        return fullPath;
    }

    private static List<string> NormalizeSegments(IEnumerable<string> segments)
    {
        var normalized = new List<string>();

        foreach (var rawSegment in segments)
        {
            if (string.IsNullOrWhiteSpace(rawSegment))
            {
                continue;
            }

            var cleaned = rawSegment.Trim().Trim('/', '\\');
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Contains("..", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Uploads path contains an invalid segment.");
            }

            normalized.Add(cleaned);
        }

        return normalized;
    }
}