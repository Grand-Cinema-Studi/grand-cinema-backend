using GrandCinema.Application.Abstractions.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GrandCinema.Api.Errors;

internal static class ErrorMappings
{
    // Translates a business failure into a Problem Details response (RFC 9457): the front chooses its French message from "type".
    public static ProblemHttpResult ToProblem(this Error error)
    {
        ProblemDetails problem = error.ValidationErrors is null
            ? new ProblemDetails()
            : new HttpValidationProblemDetails(new Dictionary<string, string[]>(error.ValidationErrors));

        problem.Type = $"/errors/{error.Code}";
        problem.Title = error.Message;
        problem.Status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(problem);
    }
}
