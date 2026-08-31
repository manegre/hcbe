namespace HcbeApi.Helpers;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse CreateSuccess(string? message = null)
    {
        var response = new ApiResponse();
        response.Success = true;
        response.Message = message;
        return response;
    }

    public static ApiResponse CreateError(string message, List<string>? errors = null)
    {
        var response = new ApiResponse();
        response.Success = false;
        response.Message = message;
        response.Errors = errors;
        return response;
    }
}

