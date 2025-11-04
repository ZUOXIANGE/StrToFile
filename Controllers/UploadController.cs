using Microsoft.AspNetCore.Mvc;
using StrToFile.Models;
using System.ComponentModel.DataAnnotations;
using StrToFile.Services;

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

            var result = await ZipParser.ParseZipFilesAsync(zipFile, _logger);
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