using Microsoft.AspNetCore.Mvc;
using StrToFile.Models;
using StrToFile.Services;
using System.ComponentModel.DataAnnotations;

namespace StrToFile.Controllers;

/// <summary>
/// 文件下载控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DownloadController : ControllerBase
{
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(ILogger<DownloadController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 主要下载接口 - 接收文件列表并返回 ZIP 压缩包
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <returns>ZIP 文件流</returns>
    [HttpPost("download-zip")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadZip([FromBody][Required] List<FileItem> files)
    {
        try
        {
            // 参数验证
            if (files == null || !files.Any())
            {
                return BadRequest(new { error = "文件列表不能为空" });
            }

            // 验证每个文件项
            var validationErrors = new List<string>();
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (string.IsNullOrWhiteSpace(file.FileName))
                {
                    validationErrors.Add($"第 {i + 1} 个文件的文件名不能为空");
                }
                if (file.Content == null)
                {
                    validationErrors.Add($"第 {i + 1} 个文件的内容不能为 null");
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new { error = "验证失败", details = validationErrors });
            }

            _logger.LogInformation("开始创建 ZIP 文件，包含 {FileCount} 个文件", files.Count);

            // 创建 ZIP 文件
            var zipBytes = await ZipCreator.CreateZipBytesAsync(files);
            var fileName = ZipCreator.GenerateZipFileName("download");

            _logger.LogInformation("ZIP 文件创建成功，大小: {Size} 字节", zipBytes.Length);

            // 返回文件
            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 ZIP 文件时发生错误");
            return StatusCode(500, new { error = "服务器内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 流式下载接口 - 使用流式处理，适合大文件
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <returns>ZIP 文件流</returns>
    [HttpPost("download-zip-stream")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadZipStream([FromBody][Required] List<FileItem> files)
    {
        try
        {
            // 参数验证
            if (files == null || !files.Any())
            {
                return BadRequest(new { error = "文件列表不能为空" });
            }

            _logger.LogInformation("开始流式创建 ZIP 文件，包含 {FileCount} 个文件", files.Count);

            var fileName = ZipCreator.GenerateZipFileName("stream_download");

            // 设置响应头
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            Response.ContentType = "application/zip";

            // 直接写入响应流
            await ZipCreator.CreateZipToStreamAsync(files, Response.Body);

            _logger.LogInformation("流式 ZIP 文件创建完成");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流式创建 ZIP 文件时发生错误");
            return StatusCode(500, new { error = "服务器内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 示例下载接口 - 返回预设的示例文件
    /// </summary>
    /// <returns>包含示例文件的 ZIP 压缩包</returns>
    [HttpGet("download-sample")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadSample()
    {
        try
        {
            _logger.LogInformation("创建示例 ZIP 文件");

            // 创建示例文件
            var sampleFiles = new List<FileItem>
            {
                new FileItem
                {
                    FileName = "readme.txt",
                    Content = "这是一个示例 README 文件。\n\n本项目提供了 ZIP 文件生成和下载功能。\n\n创建时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                },
                new FileItem
                {
                    FileName = "config/app.json",
                    Content = "{\n  \"name\": \"StrToFile API\",\n  \"version\": \"1.0.0\",\n  \"description\": \"字符串转文件 ZIP 下载服务\"\n}"
                },
                new FileItem
                {
                    FileName = "scripts/hello.cs",
                    Content = "using System;\n\nnamespace Sample\n{\n    class Program\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"Hello, World!\");\n            Console.WriteLine($\"当前时间: {DateTime.Now}\");\n        }\n    }\n}"
                },
                new FileItem
                {
                    FileName = "data/users.csv",
                    Content = "ID,姓名,邮箱,创建时间\n1,张三,zhangsan@example.com,2024-01-01\n2,李四,lisi@example.com,2024-01-02\n3,王五,wangwu@example.com,2024-01-03"
                }
            };

            // 创建 ZIP 文件
            var zipBytes = await ZipCreator.CreateZipBytesAsync(sampleFiles);
            var fileName = ZipCreator.GenerateZipFileName("sample");

            _logger.LogInformation("示例 ZIP 文件创建成功，大小: {Size} 字节", zipBytes.Length);

            // 返回文件
            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建示例 ZIP 文件时发生错误");
            return StatusCode(500, new { error = "服务器内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取 API 信息
    /// </summary>
    /// <returns>API 信息</returns>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInfo()
    {
        var info = new
        {
            name = "StrToFile API",
            version = "1.0.0",
            description = "字符串转文件 ZIP 下载服务",
            endpoints = new[]
            {
                new { method = "POST", path = "/api/download/download-zip", description = "主要下载接口" },
                new { method = "POST", path = "/api/download/download-zip-stream", description = "流式下载接口" },
                new { method = "GET", path = "/api/download/download-sample", description = "示例下载接口" },
                new { method = "GET", path = "/api/download/info", description = "API 信息接口" }
            },
            timestamp = DateTime.Now
        };

        return Ok(info);
    }
}