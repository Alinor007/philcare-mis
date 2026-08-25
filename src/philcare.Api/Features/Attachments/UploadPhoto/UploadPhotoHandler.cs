using philcare.Api.Common.Domain;
using philcare.Api.Features.Attachments.Domain;

namespace philcare.Api.Features.Attachments.UploadPhoto;

public sealed record UploadPhotoResponse(string Url, string Category, string FileName);

public sealed class UploadPhotoHandler(PhotoStore store)
{
    public async Task<Result<UploadPhotoResponse>> HandleAsync(
        string category, IFormFile? file, CancellationToken cancellationToken)
    {
        if (!PhotoRules.IsKnownCategory(category))
        {
            return Result.Failure<UploadPhotoResponse>(PhotoRules.UnknownCategory());
        }

        if (file is null || file.Length == 0)
        {
            return Result.Failure<UploadPhotoResponse>(
                Error.Validation("Photos.Missing", "No image was uploaded."));
        }

        if (file.Length > store.MaxBytes)
        {
            return Result.Failure<UploadPhotoResponse>(PhotoRules.TooLarge(store.MaxBytes));
        }

        await using var upload = file.OpenReadStream();

        // Sniff the real format from the leading bytes rather than trusting ContentType or the
        // filename, both of which the client controls.
        var header = new byte[12];
        var read = await upload.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var extension = PhotoRules.ResolveExtension(header.AsSpan(0, read));

        if (extension is null)
        {
            return Result.Failure<UploadPhotoResponse>(PhotoRules.UnsupportedFormat());
        }

        // Rewind so the sniffed header is written too. IFormFile streams are seekable (buffered to
        // disk past the in-memory threshold), so this is safe rather than needing a re-read.
        upload.Position = 0;

        var normalizedCategory = category.ToLowerInvariant();
        var fileName = await store.SaveAsync(normalizedCategory, extension, upload, cancellationToken);

        // The relative API path, not a filesystem path — this is what goes in the PhotoUrl column.
        return Result.Success(new UploadPhotoResponse(
            $"/api/photos/{normalizedCategory}/{fileName}", normalizedCategory, fileName));
    }
}
