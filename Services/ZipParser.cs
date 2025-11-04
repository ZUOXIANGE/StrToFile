using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StrToFile.Models;

namespace StrToFile.Services;

/// <summary>
/// ZIP 解析工具类：将 ZIP 文件解析为 List<FileItem>
/// </summary>
public static class ZipParser
{
    /// <summary>
    /// 解析单个 ZIP 流为文件项列表
    /// </summary>
    /// <param name="zipStream">ZIP 文件流</param>
    /// <returns>解析后的文件项列表</returns>
    /// <exception cref="ArgumentNullException">当 zipStream 为空时抛出</exception>
    public static async Task<List<FileItem>> ParseZipStreamAsync(Stream zipStream)
    {
        if (zipStream == null)
            throw new ArgumentNullException(nameof(zipStream));

        var result = new List<FileItem>();

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);
        foreach (var entry in archive.Entries)
        {
            // 目录条目没有 Name，仅有 FullName
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();

            result.Add(new FileItem
            {
                FileName = entry.FullName.Replace('\\', '/'),
                Content = content
            });
        }

        return result;
    }

    /// <summary>
    /// 解析多个 IFormFile ZIP 文件并合并结果
    /// </summary>
    /// <param name="zipFiles">上传的 ZIP 文件列表</param>
    /// <param name="logger">可选日志记录器</param>
    /// <returns>合并后的文件项列表</returns>
    /// <exception cref="ArgumentNullException">当 zipFiles 为空时抛出</exception>
    /// <exception cref="ArgumentException">当其中存在空文件时抛出</exception>
    public static async Task<List<FileItem>> ParseZipFilesAsync(IEnumerable<IFormFile> zipFiles, ILogger? logger = null)
    {
        if (zipFiles == null)
            throw new ArgumentNullException(nameof(zipFiles));

        var aggregated = new List<FileItem>();

        foreach (var file in zipFiles)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("存在空文件，请检查上传的ZIP", nameof(zipFiles));

            var fileName = file.FileName ?? string.Empty;
            if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning("上传的文件扩展名并非 .zip: {FileName}", fileName);
            }

            using var inputStream = file.OpenReadStream();
            var items = await ParseZipStreamAsync(inputStream);
            aggregated.AddRange(items);
        }

        return aggregated;
    }
}