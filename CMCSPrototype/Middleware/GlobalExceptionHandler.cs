using CMCSPrototype.Services;
using System.Net;

namespace CMCSPrototype.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingService _loggingService;

        public GlobalExceptionHandler(RequestDelegate next, ILoggingService loggingService)
        {
            _next = next;
            _loggingService = loggingService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Unhandled exception: {ex.Message}", ex);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var errorMessage = exception is InvalidOperationException 
                ? exception.Message 
                : "An unexpected error occurred. Please try again later.";

            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Error</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; padding: 50px; background: #f5f5f5; }}
                        .error-container {{ background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); max-width: 600px; margin: 0 auto; }}
                        h1 {{ color: #d9534f; }}
                        .btn {{ display: inline-block; padding: 10px 20px; background: #5bc0de; color: white; text-decoration: none; border-radius: 4px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='error-container'>
                        <h1>⚠️ Error</h1>
                        <p>{errorMessage}</p>
                        <a href='/' class='btn'>Return to Home</a>
                    </div>
                </body>
                </html>";

            return context.Response.WriteAsync(html);
        }
    }
}
