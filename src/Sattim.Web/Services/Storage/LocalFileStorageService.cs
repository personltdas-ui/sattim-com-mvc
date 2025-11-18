using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Storage
{
    public class LocalStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            var uploadsFolderPath = Path.Combine(_environment.WebRootPath, containerName);
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/{containerName}/{uniqueFileName}";
        }

        public Task DeleteFileAsync(string fileUrl, string containerName)
        {
            try
            {
                var fileName = Path.GetFileName(fileUrl);
                var filePath = Path.Combine(_environment.WebRootPath, containerName, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }
    }
}