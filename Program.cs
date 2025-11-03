using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StrToFile API",
        Version = "v1",
        Description = "字符串转文件 ZIP 下载服务 - 提供多个文件打包下载功能",
        Contact = new OpenApiContact
        {
            Name = "StrToFile API",
            Email = "support@example.com"
        }
    });

    // 包含 XML 注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 配置 CORS（如果需要跨域访问）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StrToFile API v1");
        c.RoutePrefix = string.Empty; // 设置 Swagger UI 为根路径
    });
}

// 启用 CORS
app.UseCors("AllowAll");

// 配置请求大小限制（用于处理大文件内容）
app.Use(async (context, next) =>
{
    // 设置最大请求体大小为 100MB
    context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = 100 * 1024 * 1024;
    await next();
});

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 添加根路径重定向到 Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// 添加健康检查端点
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.Now,
    version = "1.0.0"
});

app.Run();