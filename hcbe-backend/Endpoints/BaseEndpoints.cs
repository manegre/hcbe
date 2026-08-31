using HcbeApi.Helpers;

namespace HcbeApi.Endpoints;

public static class BaseEndpoints
{
    public static IResult HandleServiceResponse<T>(this ApiResponse<T> response, string? createdLocation = null)
    {
        if (!response.Success)
        {
            // If the error message indicates "not found", return 404
            if (response.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.NotFound(response);
            }
            
            return response.Errors != null && response.Errors.Any()
                ? Results.BadRequest(response)
                : Results.BadRequest(response);
        }

        if (response.Data == null)
        {
            return Results.NotFound(response);
        }

        return createdLocation != null
            ? Results.Created(createdLocation, response)
            : Results.Ok(response);
    }

    public static IResult HandleServiceResponse(this ApiResponse response)
    {
        if (!response.Success)
        {
            return response.Errors != null && response.Errors.Any()
                ? Results.BadRequest(response)
                : Results.BadRequest(response);
        }

        return Results.NoContent();
    }
}

