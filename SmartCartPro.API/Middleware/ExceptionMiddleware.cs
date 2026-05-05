using System.Net;
using System.Text.Json;
using SmartCartPro.Models.Common;

namespace SmartCartPro.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                _logger.LogWarning("AppException: {Message}", ex.Message);
                context.Response.StatusCode = ex.StatusCode;
                context.Response.ContentType = "application/json";
                var resp = ApiResponse<object>.Fail(ex.Message);
                await context.Response.WriteAsync(JsonSerializer.Serialize(resp));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                var resp = ApiResponse<object>.Fail("An unexpected error occurred.");
                await context.Response.WriteAsync(JsonSerializer.Serialize(resp));
            }
        }
    }
}