using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ProjectEvaluationSystem.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is missing or incomplete");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "project_evaluation_system",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Failed to upload image: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task DeleteImageAsync(string publicUrl)
        {
            if (string.IsNullOrEmpty(publicUrl))
            {
                return;
            }

            // Extract the public ID from the URL
            // URL format: https://res.cloudinary.com/{cloud_name}/image/upload/v{version}/{folder}/{public_id}.{format}
            var uri = new Uri(publicUrl);
            var pathSegments = uri.AbsolutePath.Split('/');
            
            // Skip the first segment (empty) and find the segment after "upload"
            int uploadIndex = Array.IndexOf(pathSegments, "upload");
            if (uploadIndex < 0 || uploadIndex + 2 >= pathSegments.Length)
            {
                return; // Not a valid Cloudinary URL or can't extract the public ID
            }

            // The public ID starts after "upload" and version (v1234567)
            // Join all remaining segments except the file extension
            string publicIdWithVersion = string.Join("/", pathSegments, uploadIndex + 1, pathSegments.Length - uploadIndex - 1);
            string publicId = publicIdWithVersion;
            
            // Remove version if present (v1234567)
            if (publicIdWithVersion.StartsWith("v") && publicIdWithVersion.IndexOf('/') > 0)
            {
                publicId = publicIdWithVersion.Substring(publicIdWithVersion.IndexOf('/') + 1);
            }
            
            // Remove file extension if present
            int lastDotIndex = publicId.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                publicId = publicId.Substring(0, lastDotIndex);
            }

            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
} 