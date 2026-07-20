using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MarketLine.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace MarketLine.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<RawUploadResult> UploadMediaAsync(IFormFile file, string folderName)
        {
            var uploadResult = new RawUploadResult();

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (ext == ".mp4" || ext == ".mov" || ext == ".webm" || ext == ".ogg")
                {
                    var uploadParams = new VideoUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = folderName,
                        Overwrite = true
                    };
                    // Use robust standard UploadAsync instead of chunked UploadLargeAsync
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                }
                else
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = folderName,
                        Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                    };
                    uploadResult = await _cloudinary.UploadAsync(uploadParams);
                }
            }

            return uploadResult;
        }

        public async Task<DeletionResult> DeleteMediaAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}
