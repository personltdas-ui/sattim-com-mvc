using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Storage
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string containerName);

        Task DeleteFileAsync(string fileUrl, string containerName);
    }
}