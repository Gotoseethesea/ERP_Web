using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ERP_Web.Core.Service
{
    // 定义一个接口，抽象文件存储逻辑
    public interface IDbStorage
    {
        Task SaveAsync(IFormFile file, string savePath);
    }

    // 实现接口，使用本地文件系统进行存储
    public class LocalFileStorage : IDbStorage
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocalFileStorage> _logger;

        public LocalFileStorage(IWebHostEnvironment env, ILogger<LocalFileStorage> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task SaveAsync(IFormFile file, string savePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, savePath);
            var directory = Path.GetDirectoryName(fullPath);

            try
            {
                Directory.CreateDirectory(directory!);
                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);
                _logger.LogInformation("文件已保存: {Path}", savePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件保存失败: {Path}", fullPath);
                throw;
            }
        }
    }
}
