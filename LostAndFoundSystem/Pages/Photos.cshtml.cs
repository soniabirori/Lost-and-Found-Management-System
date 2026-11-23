using LostAndFoundSystem.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LostAndFoundSystem.Pages
{
    // Photo upload module: upload a photo and display all uploaded photos.
    // Anyone can view the gallery. Uploading and deleting need a login.
    public class PhotosModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public PhotosModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Filled from the file input named "Photo" in the _PhotoUpload partial
        [BindProperty] public IFormFile? Photo { get; set; }

        public List<PhotoInfo> Photos { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public void OnGet()
        {
            SuccessMessage = TempData["SuccessMessage"] as string ?? "";
            LoadPhotos();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var result = await PhotoHelper.SaveAsync(Photo, _environment.WebRootPath, HttpContext.RequestAborted);

            if (!result.Success)
            {
                ErrorMessage = result.Error!;
                LoadPhotos();
                return Page();
            }

            TempData["SuccessMessage"] = "Photo uploaded successfully.";
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string? photoPath)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            PhotoHelper.Delete(photoPath, _environment.WebRootPath);
            TempData["SuccessMessage"] = "Photo deleted.";
            return RedirectToPage();
        }

        private void LoadPhotos()
        {
            Photos = PhotoHelper.GetAll(_environment.WebRootPath);
        }
    }
}
