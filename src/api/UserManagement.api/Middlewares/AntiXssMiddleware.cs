using System.Text;
using System.Text.Json;
using Ganss.Xss;
using UserManagement.Helper;

namespace UserManagement.Middlewares
{
    public class AntiXssMiddleware
    {
        private readonly RequestDelegate _next;

        public AntiXssMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
            {
            var response = new APIResponseClass<bool>();
            var sanitizer = new HtmlSanitizer();
            // Sanitize request body
            if (httpContext.Request.ContentType == "application/json")
            {
                httpContext.Request.EnableBuffering();

                using (var streamReader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    var rawBody = await streamReader.ReadToEndAsync();
                    var sanitizedBody = sanitizer.Sanitize(rawBody);

                    if (rawBody != sanitizedBody)
                    {
                        response.message = "XSS injection detected in JSON request body.";
                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.result = false;

                        httpContext.Response.StatusCode = StatusCodes.Status200OK;

                        if (!httpContext.Response.Headers.ContainsKey("access-control-allow-origin"))
                        {
                            httpContext.Response.Headers.Append("access-control-allow-origin", "*");
                        }

                        httpContext.Response.ContentType = "application/json; charset=utf-8";
                        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
                        return;
                    }
                }

                httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            }

            // Sanitize query parameters
            foreach (var key in httpContext.Request.Query.Keys)
            {
                var value = httpContext.Request.Query[key].ToString();
                var sanitizedValue = sanitizer.Sanitize(value);

                if (value != sanitizedValue)
                {
                    response.message = $"XSS injection detected in query parameter: {key}";
                    response.apiResponseStatus = Enum.APIResponseStatus.Error;
                    response.result = false;

                    httpContext.Response.StatusCode = StatusCodes.Status200OK;

                    if (!httpContext.Response.Headers.ContainsKey("access-control-allow-origin"))
                    {
                        httpContext.Response.Headers.Append("access-control-allow-origin", "*");
                    }

                    httpContext.Response.ContentType = "application/json; charset=utf-8";
                    await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }

            // Sanitize form data
            if (httpContext.Request.HasFormContentType)
            {
                httpContext.Request.EnableBuffering();
                var formCollection = await httpContext.Request.ReadFormAsync();

                foreach (var key in formCollection.Keys)
                {
                    var value = formCollection[key].ToString();
                    var sanitizedValue = sanitizer.Sanitize(value);

                    if (value != sanitizedValue)
                    {
                        response.message = $"XSS injection detected in form parameter: {key}";
                        response.apiResponseStatus = Enum.APIResponseStatus.Error;
                        response.result = false;

                        httpContext.Response.StatusCode = StatusCodes.Status200OK;

                        if (!httpContext.Response.Headers.ContainsKey("access-control-allow-origin"))
                        {
                            httpContext.Response.Headers.Append("access-control-allow-origin", "*");
                        }

                        httpContext.Response.ContentType = "application/json; charset=utf-8";
                        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
                        return;
                    }
                }

                // Reset the request body position to avoid infinite looping
                httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            }

            // Pass control to the next middleware
            await _next(httpContext);
        }
    }

    public static class AntiXssMiddlewareExtensions
    {
        public static IApplicationBuilder UseAntiXssMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AntiXssMiddleware>();
        }
    }
}
