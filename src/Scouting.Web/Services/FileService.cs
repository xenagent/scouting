using Microsoft.AspNetCore.Components.Forms;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IFileService
{
    Task<ServiceResult<string>> UploadPlayerImageAsync(IBrowserFile file, CancellationToken ct = default);
}

public class FileService : IFileService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _env;

    public FileService(IWebHostEnvironment env) => _env = env;

    public async Task<ServiceResult<string>> UploadPlayerImageAsync(IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return ServiceResult<string>.Fail("FILE_INVALID_TYPE");

        if (file.Size > MaxFileSizeBytes)
            return ServiceResult<string>.Fail("FILE_TOO_LARGE");

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "players");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var readStream = file.OpenReadStream(MaxFileSizeBytes, ct);
        await using var writeStream = File.Create(filePath);
        await readStream.CopyToAsync(writeStream, ct);

        return ServiceResult<string>.Ok($"/uploads/players/{fileName}");
    }
}
