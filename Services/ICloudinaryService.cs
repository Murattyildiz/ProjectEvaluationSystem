using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ProjectEvaluationSystem.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task DeleteImageAsync(string publicId);
    }
} 