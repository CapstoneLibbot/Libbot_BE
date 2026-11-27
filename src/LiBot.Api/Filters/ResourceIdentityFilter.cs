using LiBot.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LiBot.Api.Filters;

public sealed class ResourceIdentityFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var resource = context.ActionArguments.Values.FirstOrDefault(IsResourceDto);
        if (resource is null)
        {
            await next();
            return;
        }

        var id = resource.GetType().GetProperty("Id")?.GetValue(resource);
        var requestId = id is Guid value ? value : Guid.Empty;
        var method = context.HttpContext.Request.Method;

        if (HttpMethods.IsPost(method) && requestId != Guid.Empty)
        {
            context.Result = BadRequest("Không được tự truyền Id khi tạo dữ liệu; hệ thống sẽ tự sinh Id.");
            return;
        }

        if (HttpMethods.IsPut(method)
            && requestId != Guid.Empty
            && context.RouteData.Values.TryGetValue("id", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeId)
            && requestId != routeId)
        {
            context.Result = BadRequest("Id trong dữ liệu phải trùng với Id trên đường dẫn.");
            return;
        }

        await next();
    }

    private static bool IsResourceDto(object? value) => value is not null
        && value.GetType().GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType
            && interfaceType.GetGenericTypeDefinition() == typeof(ILibraryResourceDto<>));

    private static BadRequestObjectResult BadRequest(string message) =>
        new(new BaseResponse<object>(null, message, false));
}
