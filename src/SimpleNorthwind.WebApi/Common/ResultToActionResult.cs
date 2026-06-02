using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.WebApi.Common;

/// <summary>Result / Result&lt;T&gt; → HTTP 對映（ProblemDetails，RFC 7807），見 06-稽核與共通技術規範#6。</summary>
public static class ResultToActionResult
{
    public static IActionResult ToProblem(this Result result) => CreateProblem(result.Error);

    public static ActionResult<T> ToOk<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);
        return CreateProblem(result.Error);
    }

    public static ActionResult<T> ToActionResult<T>(this Result<T> result, Func<T, ActionResult<T>> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);
        return CreateProblem(result.Error);
    }

    private static ObjectResult CreateProblem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
