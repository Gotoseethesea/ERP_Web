using ERP_Web.Models;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using M = ERP_Web.Models;
using ERP_Web.Core.Service;
using ERP_Web.Repository;


namespace ERP_Web.Core.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        // 注入环境变量、数据库上下文和文件存储服务
        private readonly IWebHostEnvironment _env;
        private readonly SqlClient _db;
        private readonly IDbStorage _dbStorage;

        public UploadController(
            IWebHostEnvironment env,
            SqlClient db,
            IDbStorage dbStorage)
        {
            _env = env;
            _db = db;
            _dbStorage = dbStorage;
        }

        /// <summary>
        /// 文件上传接口 api/Upload/FileUpload
        /// </summary>
        [HttpPost("FileUpload")]
        public async Task<IActionResult> FileUpload(
            IFormFile file,
            [FromForm] string refCode,
            [FromForm] string refType)
        {
            if (file == null || file.Length == 0)
                return BadRequest("无效的文件");

            try
            {
                // 安全路径生成
                var savePath = GenerateFilePath(file.FileName);
                var entity = new SysAttachment
                {
                    RefCode = refCode,
                    RefType = refType,
                    Name = file.FileName,
                    Path = savePath,
                    Type = Path.GetExtension(file.FileName),
                    Size = file.Length
                };

                // 使用事务确保原子性
                var tranResult = await _db.Db.Ado.UseTranAsync(async () =>
                {
                    // 1. 保存物理文件
                    await _dbStorage.SaveAsync(file, savePath);
                    // 2. 插入数据库记录
                    entity.Id = await _db.Db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
                });

                if (!tranResult.IsSuccess)
                {
                    // 事务失败时删除可能已写入的文件
                    var fullPath = Path.Combine(_env.WebRootPath, savePath);
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                    return StatusCode(500, "文件上传失败");
                }

                return Ok(new { id = entity.Id, name = entity.Name });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"服务器错误: {ex.Message}");
            }
        }



        /// <summary>
        /// 图片上传
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        //[HttpPost("ImageUpload")]
        //[Route("/ImageUpload")]
        [HttpPost("ImageUpload")] // 使用明确的路由
        public async Task<IActionResult> ImageUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("无效的文件");

            // 生成唯一文件名
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "inventory");

            // 确保目录存在
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 返回图片访问URL
            var imageUrl = $"/uploads/inventory/{fileName}";
            return Ok(new { url = imageUrl });
        }

        // 获取已存在的附件
        [HttpGet("GetAttachments")]
        public async Task<IActionResult> GetAttachments([FromQuery] string refCode, [FromQuery] string refType)
        {
            var attachments = await _db.Db.Queryable<SysAttachment>()
                .Where(a => a.RefCode == refCode && a.RefType == refType)
                .ToListAsync();

            return Ok(attachments);
        }

        // 文件下载接口
        [HttpGet("Download/{id}")]
        public async Task<IActionResult> Download(long id)
        {
            var entity = await _db.Db.Queryable<SysAttachment>().FirstAsync(x => x.Id == id);
            if (entity == null) return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, entity.Path);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var stream = new FileStream(fullPath, FileMode.Open);
            return File(stream, "application/octet-stream", entity.Name);
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _db.Db.Queryable<SysAttachment>().FirstAsync(x => x.Id == id);
            if (entity == null)
                return NotFound("附件不存在");

            try
            {
                // 事务：删除物理文件 + 数据库记录
                var tranResult = await _db.Db.Ado.UseTranAsync(async () =>
                {
                    // 1. 删除物理文件
                    var fullPath = Path.Combine(_env.WebRootPath, entity.Path);
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                    // 2. 删除数据库记录
                    await _db.Db.Deleteable<SysAttachment>().Where(x => x.Id == id).ExecuteCommandAsync();
                });

                return tranResult.IsSuccess ? Ok() : StatusCode(500, "删除失败");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"删除失败: {ex.Message}");
            }
        }

        private string GenerateFilePath(string fileName)
        {
            var datePath = DateTime.Now.ToString("yyyy/MM/dd");
            var safeFileName = Path.GetFileName(fileName); // 过滤路径攻击
            return Path.Combine("attachments", datePath, $"{Guid.NewGuid()}_{safeFileName}");
        }
    }
}

