using HcbeApi.Helpers;

namespace HcbeApi.Helpers;

public static class ResultExtensions
{
    public static IResult ToResult<T>(this ApiResponse<T> response)
    {
        if (response.Success)
        {
            return response.Data != null 
                ? Results.Ok(response) 
                : Results.Ok(response);
        }

        return response.Errors != null && response.Errors.Any()
            ? Results.BadRequest(response)
            : Results.BadRequest(response);
    }

    public static IResult ToCreatedResult<T>(this ApiResponse<T> response, string location)
    {
        if (response.Success && response.Data != null)
        {
            return Results.Created(location, response);
        }

        return response.ToResult();
    }

    public static IResult ToNotFoundResult<T>(this ApiResponse<T> response)
    {
        return response.Success 
            ? Results.NotFound(response) 
            : response.ToResult();
    }
}

