using System.IO.Compression;
using System.Text;
using StrToFile.Models;

namespace StrToFile.Services;

/// <summary>
/// ZIP 文件创建器，提供多种压缩方式
/// </summary>
public static class ZipCreator
{
    /// <summary>
    /// 同步版本 - 返回字节数组
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <returns>ZIP 文件字节数组</returns>
    /// <exception cref="ArgumentNullException">文件列表为空时抛出</exception>
    public static byte[] CreateZipBytes(IEnumerable<FileItem> files)
    {
        if (files == null)
            throw new ArgumentNullException(nameof(files));

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.FileName))
                    continue;

                // 清理文件名，防止目录遍历攻击
                var safeFileName = SanitizeFileName(file.FileName);
                    
                var entry = archive.CreateEntry(safeFileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(file.Content);
            }
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// 异步版本 - 返回字节数组
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <returns>ZIP 文件字节数组</returns>
    /// <exception cref="ArgumentNullException">文件列表为空时抛出</exception>
    public static async Task<byte[]> CreateZipBytesAsync(IEnumerable<FileItem> files)
    {
        if (files == null)
            throw new ArgumentNullException(nameof(files));

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.FileName))
                    continue;

                // 清理文件名，防止目录遍历攻击
                var safeFileName = SanitizeFileName(file.FileName);
                    
                var entry = archive.CreateEntry(safeFileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(file.Content);
            }
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// 流式版本 - 直接写入输出流（适合大文件）
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <param name="outputStream">输出流</param>
    /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
    public static async Task CreateZipToStreamAsync(IEnumerable<FileItem> files, Stream outputStream)
    {
        if (files == null)
            throw new ArgumentNullException(nameof(files));
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));

        using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, true);
            
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FileName))
                continue;

            // 清理文件名，防止目录遍历攻击
            var safeFileName = SanitizeFileName(file.FileName);
                
            var entry = archive.CreateEntry(safeFileName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            await writer.WriteAsync(file.Content);
        }
    }

    /// <summary>
    /// 清理文件名，防止目录遍历攻击
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>安全的文件名</returns>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "untitled.txt";

        // 移除危险字符和路径
        var sanitized = fileName
            .Replace("..", "")
            .Replace("\\", "/")
            .Trim('/', ' ');

        // 如果清理后为空，使用默认名称
        if (string.IsNullOrWhiteSpace(sanitized))
            return "untitled.txt";

        return sanitized;
    }

    /// <summary>
    /// 生成带时间戳的 ZIP 文件名
    /// </summary>
    /// <param name="prefix">文件名前缀</param>
    /// <returns>带时间戳的文件名</returns>
    public static string GenerateZipFileName(string prefix = "files")
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{prefix}_{timestamp}.zip";
    }
}