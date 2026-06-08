using System.Text.Json;
using IshikawaRca.Contracts.Common;

namespace IshikawaRca.Web.Api;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
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
        catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api"))
        {
            _logger.LogError(ex, "Unhandled API exception. TraceIdentifier: {TraceIdentifier}", context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var result = ApiResult<object>.Fail(
                "Error interno al procesar la solicitud.",
                new ApiError
                {
                    Field = string.Empty,
                    Code = "UNHANDLED_API_ERROR",
                    Message = "La operacion no pudo completarse. Usa el correlationId para soporte."
                });
            result.CorrelationId = context.TraceIdentifier;

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                result,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
    }
}

