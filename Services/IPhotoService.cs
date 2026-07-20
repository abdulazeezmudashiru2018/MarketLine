using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using CloudinaryDotNet.Actions;

namespace MarketLine.Services
{
    public interface IPhotoService
    {
        Task<RawUploadResult> UploadMediaAsync(IFormFile file, string folderName);
        Task<DeletionResult> DeleteMediaAsync(string publicId);
    }
}
