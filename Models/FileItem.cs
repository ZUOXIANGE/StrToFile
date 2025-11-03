using System.ComponentModel.DataAnnotations;

namespace StrToFile.Models;

/// <summary>
/// 文件项数据模型，用于前后端数据传递
/// </summary>
public class FileItem
{
    /// <summary>
    /// 文件名（支持路径，如 "folder/file.txt"）
    /// </summary>
    [Required(ErrorMessage = "文件名不能为空")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件内容（文本格式）
    /// </summary>
    [Required(ErrorMessage = "文件内容不能为空")]
    public string Content { get; set; } = string.Empty;
}