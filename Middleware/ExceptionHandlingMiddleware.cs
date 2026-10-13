using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private const int SlowRequestMs = 3000;   // NFR-PER-001: quá 3 giây thì cảnh báo

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        catch (DbUpdateException ex)   // vi phạm ràng buộc CSDL: trùng dữ liệu, sai khóa ngoại...
        {
            _logger.LogError(ex, "Lỗi cập nhật CSDL. TraceId={TraceId}", context.TraceIdentifier);
            await WriteAsync(context, HttpStatusCode.Conflict,
                "Dữ liệu bị trùng hoặc không hợp lệ. Vui lòng kiểm tra lại và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xử lý được. TraceId={TraceId}", context.TraceIdentifier);
            await WriteAsync(context, HttpStatusCode.InternalServerError,
                "Đã có lỗi xảy ra, vui lòng thử lại sau.");
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > SlowRequestMs)
                _logger.LogWarning("Request chậm: {Method} {Path} mất {Elapsed} ms",
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task WriteAsync(HttpContext context, HttpStatusCode code, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        // traceId để nhân viên hỗ trợ tra đúng dòng log của lỗi này
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, traceId = context.TraceIdentifier }));
    }
}