namespace LostAndFoundSystem.Helpers
{
    // Outcome of a photo upload: either a saved path or a friendly error message
    public record PhotoUploadResult(bool Success, string? Path, string? Error)
    {
        public static PhotoUploadResult Ok(string path) => new(true, path, null);
        public static PhotoUploadResult Fail(string error) => new(false, null, error);
    }

    // One saved photo, used to show the gallery
    public record PhotoInfo(string Path, DateTime UploadedAt);

    // Options for the _PhotoUpload partial view
    public class PhotoUploadOptions
    {
        // Name of the form field. Must match the IFormFile property on the page model.
        public string FieldName { get; set; } = "Photo";
    }

    public static class PhotoHelper
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        public const string AllowedExtensionsText = "JPG, PNG or WEBP";

        // Folder (inside wwwroot) where photos are stored, and the URL prefix that matches it
        private const string UploadFolder = "uploads/items";
        private const string UrlPrefix = "/" + UploadFolder + "/";

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Validates the file, saves it under a random name and returns its web path
        // (for example "/uploads/items/3f2a....jpg"), ready to store in Items.PhotoPath.
        public static async Task<PhotoUploadResult> SaveAsync(
            IFormFile? file, string webRootPath, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return PhotoUploadResult.Fail("Please choose a photo to upload.");

            if (file.Length > MaxFileSizeBytes)
                return PhotoUploadResult.Fail($"The photo is too large. The maximum size is {MaxFileSizeBytes / (1024 * 1024)} MB.");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return PhotoUploadResult.Fail($"Only {AllowedExtensionsText} photos are allowed.");

            // Check the real file content, not just the name, so renamed files are rejected
            if (!await HasValidSignatureAsync(file, extension, cancellationToken))
                return PhotoUploadResult.Fail("This file is not a valid photo.");

            // A random name avoids clashes and ignores whatever the user called the file
            string fileName = Guid.NewGuid().ToString("N") + (extension == ".jpeg" ? ".jpg" : extension);
            string folderPath = Path.Combine(webRootPath, "uploads", "items");
            Directory.CreateDirectory(folderPath);

            await using var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            return PhotoUploadResult.Ok(UrlPrefix + fileName);
        }

        // Lists every saved photo, newest first
        public static List<PhotoInfo> GetAll(string webRootPath)
        {
            string folderPath = Path.Combine(webRootPath, "uploads", "items");
            if (!Directory.Exists(folderPath))
                return new List<PhotoInfo>();

            return new DirectoryInfo(folderPath)
                .EnumerateFiles()
                .Where(f => AllowedExtensions.Contains(f.Extension.ToLowerInvariant()))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new PhotoInfo(UrlPrefix + f.Name, f.CreationTime))
                .ToList();
        }

        // Deletes a photo that was saved by SaveAsync. Anything outside the upload folder is ignored.
        public static void Delete(string? photoPath, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath) ||
                !photoPath.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
                return;

            // GetFileName strips any "../" so the delete can never leave the upload folder
            string fileName = Path.GetFileName(photoPath);
            string fullPath = Path.Combine(webRootPath, "uploads", "items", fileName);

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        // Compares the first bytes of the file with the known header of each image type
        private static async Task<bool> HasValidSignatureAsync(
            IFormFile file, string extension, CancellationToken cancellationToken)
        {
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            int read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < header.Length) return false;

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
                _ => false
            };
        }
    }
}
