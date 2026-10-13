using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BrandsStore.Helpers
{
    public static class FileUploadHelper
    {
        public static async Task<string> UploadImage(
            IFormFile file,
            IWebHostEnvironment webHostEnvironment,
            string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!Array.Exists(allowedExtensions, ext => ext == extension))
                {
                    throw new InvalidOperationException($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
                }

                // Validate file size (5MB max)
                if (file.Length > 5 * 1024 * 1024)
                {
                    throw new InvalidOperationException("File size must be less than 5MB");
                }

                // Create unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                // Create folder path
                var uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", folderName);

                // Ensure directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Full file path
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return relative path for database (URL path)
                return $"/images/{folderName}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file: {ex.Message}", ex);
            }
        }

        public static void DeleteImage(string imageUrl, IWebHostEnvironment webHostEnvironment)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            try
            {
                // Convert URL path to physical path
                var imagePath = imageUrl.TrimStart('/').Replace("/", "\\");
                var fullPath = Path.Combine(webHostEnvironment.WebRootPath, imagePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - deletion failure shouldn't break the app
                Console.WriteLine($"Error deleting image: {ex.Message}");
            }
        }
    }
}