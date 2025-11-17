using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Sattim.Web.Services.Storage
{
    /// <summary>
    /// Dosya yükleme (Azure Blob, AWS S3, Lokal Disk) işlemlerini soyutlar.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Bir dosyayı (resim) kaydeder ve erişilebilir URL'ini döndürür.
        /// </summary>
        /// <param name="file">Controller'dan gelen IFormFile</param>
        /// <param name="containerName">Kaydedilecek klasör/container (örn: "product-images")</param>
        /// <returns>Kaydedilen dosyanın tam URL'i</returns>
        Task<string> UploadFileAsync(IFormFile file, string containerName);

        /// <summary>
        /// Bir dosyayı URL'inden bularak siler.
        /// </summary>
        Task DeleteFileAsync(string fileUrl, string containerName);
    }
}