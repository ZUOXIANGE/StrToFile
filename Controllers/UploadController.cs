using Microsoft.AspNetCore.Mvc;
using StrToFile.Models;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text;

namespace StrToFile.Controllers;

/// <summary>
/// 文件上传与解析控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UploadController : ControllerBase
{
    private readonly ILogger<UploadController> _logger;

    public UploadController(ILogger<UploadController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 上传一个 ZIP 文件并解析为文件列表
    /// </summary>
    /// <param name="zipFile">ZIP 压缩文件（multipart/form-data）</param>
    /// <returns>解析得到的文件列表（文件名 + 文本内容）</returns>
    [HttpPost("parse-zip")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(List<FileItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ParseZip([FromForm][Required] List<IFormFile> zipFile)
    {
        try
        {
            if (zipFile == null || !zipFile.Any())
            {
                return BadRequest(new { error = "上传文件不能为空" });
            }

            var result = new List<FileItem>();

            foreach (var file in zipFile)
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "存在空文件，请检查上传的ZIP" });
                }

                var fileName = file.FileName ?? string.Empty;
                if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("上传的文件扩展名并非 .zip: {FileName}", fileName);
                }

                using var inputStream = file.OpenReadStream();
                using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read, leaveOpen: false);

                foreach (var entry in archive.Entries)
                {
                    // 目录条目没有 Name，仅有 FullName
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    using var entryStream = entry.Open();
                    // 尝试按 UTF-8（含 BOM）读取为文本内容
                    using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var content = await reader.ReadToEndAsync();

                    result.Add(new FileItem
                    {
                        FileName = entry.FullName.Replace('\\', '/'),
                        Content = content
                    });
                }
            }

            _logger.LogInformation("ZIP 解析完成，得到 {Count} 个文件项，源ZIP数量 {ZipCount}", result.Count, zipFile.Count);
            return Ok(result);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "解析 ZIP 文件时发生格式错误");
            return BadRequest(new { error = "ZIP 文件格式无效或已损坏", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析 ZIP 文件时发生错误");
            return StatusCode(500, new { error = "服务器内部错误", message = ex.Message });
        }
    }
}