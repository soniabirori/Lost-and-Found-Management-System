namespace LostAndFoundSystem.Helpers
{
    public static class PhotoHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        // Returns an error message, or null if the photo is OK
        public static string? Validate(IFormFile photo)
        {
            string ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return "Photo must be a JPG, PNG, GIF or WEBP image.";
            if (photo.Length > MaxBytes)
                return "Photo must be smaller than 5 MB.";
            return null;
        }

        // Saves the photo in wwwroot/uploads and returns its web path, e.g. "/uploads/abc.jpg"
        public static string Save(IFormFile photo, string webRootPath)
        {
            string folder = Path.Combine(webRootPath, "uploads");
            Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName).ToLowerInvariant();
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                photo.CopyTo(stream);
            }
            return "/uploads/" + fileName;
        }

        // Deletes a photo file from wwwroot/uploads (if it exists)
        public static void Delete(string? photoPath, string webRootPath)
        {
            if (string.IsNullOrEmpty(photoPath) || !photoPath.StartsWith("/uploads/"))
                return;

            string fullPath = Path.Combine(webRootPath, "uploads", Path.GetFileName(photoPath));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}