namespace Mockery.Services;

public interface IContentTypeResolver
{
    string GetContentType(string fileExtension);
}

public class ContentTypeResolver : IContentTypeResolver
{
    private static readonly Dictionary<string, string> _contentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".json", "application/json" },
        { ".html", "text/html" },
        { ".xml", "application/xml" },
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
        { ".pdf", "application/pdf" },
        { ".js", "application/javascript" },
        { ".css", "text/css" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".svg", "image/svg+xml" }
    };

    public string GetContentType(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
        {
            return "application/octet-stream";
        }

        // Ensure extension starts with a dot
        if (!fileExtension.StartsWith("."))
        {
            fileExtension = "." + fileExtension;
        }

        return _contentTypeMap.TryGetValue(fileExtension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
