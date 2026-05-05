namespace SmartCartPro.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("=> {Method} {Path}", context.Request.Method, context.Request.Path);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _next(context);
            sw.Stop();
            _logger.LogInformation("<= {Status} {Ms}ms", context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
    }
}