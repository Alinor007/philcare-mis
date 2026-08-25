using Microsoft.Extensions.Options;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Attachments.Domain;

/// <summary>
/// Reads and writes photo files under the configured storage root. The only component here that
/// touches the filesystem, so every path this app builds is assembled in one place and every one
/// of them goes through <see cref="PhotoRules.IsSafeFileName"/> first.
/// </summary>
public sealed class PhotoStore
{
    private readonly PhotoStorageOptions _options;
    private readonly string _rootPath;

    public PhotoStore(IOptions<PhotoStorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath);
    }

    public long MaxBytes => _options.MaxBytes;

    /// <summary>
    /// Writes the stream and returns the generated file name. The caller has already validated
    /// size and format; this only persists.
    /// </summary>
    public async Task<string> SaveAsync(string category, string extension, Stream content, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_rootPath, category.ToLowerInvariant());
        Directory.CreateDirectory(directory);

        var fileName = PhotoRules.NewFileName(extension);

        await using var target = File.Create(Path.Combine(directory, fileName));
        await content.CopyToAsync(target, cancellationToken);

        return fileName;
    }

    /// <summary>
    /// Opens a stored photo for reading, or fails when the name is not one this app generated or
    /// the file is simply gone. Never throws for a bad name — a hostile path is a 404, not a 500.
    /// </summary>
    public Result<PhotoContent> Open(string category, string fileName)
    {
        if (!PhotoRules.IsKnownCategory(category) || !PhotoRules.IsSafeFileName(fileName))
        {
            return Result.Failure<PhotoContent>(Error.NotFound("Photos.NotFound", "Photo not found."));
        }

        var path = Path.Combine(_rootPath, category.ToLowerInvariant(), fileName);

        if (!File.Exists(path))
        {
            return Result.Failure<PhotoContent>(Error.NotFound("Photos.NotFound", "Photo not found."));
        }

        return Result.Success(new PhotoContent(
            File.OpenRead(path),
            PhotoRules.ContentTypeFor(Path.GetExtension(fileName))));
    }
}

public sealed record PhotoContent(Stream Stream, string ContentType);
