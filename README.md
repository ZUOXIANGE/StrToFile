# StrToFile API

字符串转文件 ZIP 下载服务 - 基于 .NET 8 的 Web API，用于接收多个文件（文件名 + 内容），动态生成 ZIP 压缩文件并直接返回给前端下载。

## 功能特性

- ✅ **内存操作**: 所有操作在内存中进行，不产生临时文件
- ✅ **多种压缩方式**: 提供同步、异步和流式三种压缩方法
- ✅ **安全处理**: 自动清理文件名，防止目录遍历攻击
- ✅ **错误处理**: 完善的参数验证和异常处理
- ✅ **性能优化**: 支持异步操作和流式处理
- ✅ **API 文档**: 集成 Swagger UI 文档

## 技术栈

- **框架**: .NET 8 Web API
- **压缩库**: System.IO.Compression
- **文档**: Swagger/OpenAPI
- **日志**: Microsoft.Extensions.Logging

## API 端点

### 1. POST /api/download/download-zip
**主要下载接口**

接收文件列表并返回 ZIP 压缩包。

**请求体示例**:
```json
[
  {
    "fileName": "document1.txt",
    "content": "这是第一个文件的内容"
  },
  {
    "fileName": "data/config.json",
    "content": "{\"name\": \"test\"}"
  },
  {
    "fileName": "scripts/main.cs",
    "content": "Console.WriteLine(\"Hello\");"
  }
]
```

### 2. POST /api/download/download-zip-stream
**流式下载接口**

功能同上，但使用流式处理，适合处理大量文件或大文件内容。

### 3. GET /api/download/download-sample
**示例下载接口**

无需参数，返回预设的示例文件，用于测试和演示。

### 4. GET /api/download/info
**API 信息接口**

返回 API 的基本信息和端点列表。

### 5. POST /api/upload/parse-zip
**上传ZIP并解析为文件列表**

接收一个 `multipart/form-data` 的 ZIP 文件，解析其中的每个文件为 `FileItem`（包含 `fileName` 与 `content` 字段），返回 `List<FileItem>`。

**curl 示例**:
```bash
curl -X POST "http://localhost:5000/api/upload/parse-zip" \
  -H "Accept: application/json" \
  -F "zipFile=@./files.zip" \
  -o parsed.json
```

**返回示例**:
```json
[
  {
    "fileName": "readme.txt",
    "content": "这是ZIP中的readme内容..."
  },
  {
    "fileName": "config/app.json",
    "content": "{\n  \"name\": \"StrToFile API\"\n}"
  }
]
```

支持一次上传多个ZIP文件（字段名均为 `zipFile`）：

```bash
curl -X POST "http://localhost:5000/api/upload/parse-zip" \
  -H "Accept: application/json" \
  -F "zipFile=@./a.zip" \
  -F "zipFile=@./b.zip" \
  -o parsed.json
```

## 快速开始

### 1. 运行项目

```bash
# 还原依赖
dotnet restore

# 运行项目
dotnet run
```

### 2. 访问 API 文档

项目启动后，访问 `https://localhost:5001` 或 `http://localhost:5000` 即可看到 Swagger UI 文档。

### 3. 测试 API

#### 使用 curl 测试

```bash
# 测试示例下载
curl -X GET "https://localhost:5001/api/download/download-sample" -o sample.zip

# 测试自定义文件下载
curl -X POST "https://localhost:5001/api/download/download-zip" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "fileName": "test.txt",
      "content": "Hello World!"
    }
  ]' \
  -o custom.zip

# 测试上传ZIP并解析
curl -X POST "https://localhost:5001/api/upload/parse-zip" \
  -H "Accept: application/json" \
  -F "zipFile=@./files.zip"

# 测试一次上传多个ZIP并解析
curl -X POST "https://localhost:5001/api/upload/parse-zip" \
  -H "Accept: application/json" \
  -F "zipFile=@./a.zip" \
  -F "zipFile=@./b.zip"
```

#### 使用 JavaScript 测试

```javascript
// 下载自定义文件
const files = [
  {
    fileName: "readme.txt",
    content: "这是一个测试文件"
  },
  {
    fileName: "config/app.json",
    content: JSON.stringify({ name: "test app" }, null, 2)
  }
];

fetch('/api/download/download-zip', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(files)
})
.then(response => response.blob())
.then(blob => {
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'files.zip';
  a.click();
});
```

## 项目结构

```
StrToFile/
├── Controllers/
│   └── DownloadController.cs    # API 控制器
├── Models/
│   └── FileItem.cs             # 文件项数据模型
├── Services/
│   └── ZipCreator.cs           # ZIP 创建服务
├── Program.cs                  # 应用程序入口
├── StrToFile.csproj           # 项目文件
└── README.md                  # 项目说明
```

## 核心类说明

### FileItem 类
```csharp
public class FileItem
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
```

### ZipCreator 类
提供三种压缩方法：
- `CreateZipBytes()` - 同步版本，返回字节数组
- `CreateZipBytesAsync()` - 异步版本，返回字节数组
- `CreateZipToStreamAsync()` - 流式版本，直接写入输出流

## 安全特性

1. **文件名清理**: 自动移除 `..` 等危险字符，防止目录遍历攻击
2. **参数验证**: 完善的输入验证和错误处理
3. **请求大小限制**: 配置最大请求体大小（默认 100MB）
4. **CORS 配置**: 可配置跨域访问策略

## 性能优化

1. **异步操作**: 所有 I/O 操作都使用异步方法
2. **流式处理**: 支持直接写入响应流，减少内存占用
3. **资源管理**: 使用 `using` 语句确保资源正确释放
4. **压缩优化**: 使用 `CompressionLevel.Optimal` 获得最佳压缩效果

## 许可证

MIT License