using System.Text.Json;
using IshikawaRca.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace IshikawaRca.Web.Api;

public class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteApiErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                "No autorizado para ejecutar la operacion.",
                "FORBIDDEN",
                "El usuario no tiene el rol requerido para esta operacion.");
            return;
        }

        if (authorizeResult.Challenged)
        {
            await WriteApiErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Autenticacion requerida.",
                "AUTHENTICATION_REQUIRED",
                "La operacion requiere autenticacion valida.");
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static async Task WriteApiErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        string code,
        string errorMessage)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var result = ApiResult<object>.Fail(
            message,
            new ApiError
            {
                Field = string.Empty,
                Code = code,
                Message = errorMessage
            });
        result.CorrelationId = context.TraceIdentifier;

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            result,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            context.RequestAborted);
    }
}
