using IshikawaRca.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IshikawaRca.Web.Api;

public static class ApiErrorResponseFactory
{
    public static BadRequestObjectResult ValidationProblem(ActionContext context)
    {
        var errors = context.ModelState
            .Where(x => x.Value?.ValidationState == ModelValidationState.Invalid)
            .SelectMany(x => x.Value!.Errors.Select(error => new ApiError
            {
                Field = x.Key,
                Code = "MODEL_VALIDATION_ERROR",
                Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "El valor enviado no es valido."
                    : error.ErrorMessage
            }))
            .ToArray();

        var result = ApiResult<object>.Fail("La solicitud no es valida.", errors);
        result.CorrelationId = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(result);
    }
}

