using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LiBot.Api.Middleware;

public sealed class ApiErrorResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult result && result.StatusCode is >= 400 and <= 599)
        {
            var statusCode = result.StatusCode.Value;
            context.Result = new ObjectResult(
                ApiErrorFormatter.Normalize(statusCode, result.Value, context.HttpContext.Request.Path))
            {
                StatusCode = statusCode,
                ContentTypes = result.ContentTypes
            };
        }

        await next();
    }
}
