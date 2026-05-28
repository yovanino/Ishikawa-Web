namespace IshikawaRca.Contracts.Common;

public class ApiResult<T>
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }

    public List<ApiError> Errors { get; set; } = new();

    public string? CorrelationId { get; set; }

    public static ApiResult<T> Ok(T data, string? message = null)
    {
        return new ApiResult<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResult<T> Fail(string message, params ApiError[] errors)
    {
        return new ApiResult<T>
        {
            Success = false,
            Message = message,
            Errors = errors.ToList()
        };
    }
}
