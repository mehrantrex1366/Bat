using System.Net;

namespace Bat.AspNetCore;

public class BatExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BatExceptionHandlingMiddleware> _logger;

    public BatExceptionHandlingMiddleware(RequestDelegate next, ILogger<BatExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var requestBody = await context.Request.ReadRequestBody();
        // Message template (not an interpolated string) so the values become structured log properties
        // and the template is not re-parsed for every distinct message.
        _logger.LogError(ex, "Url: {Url}, QueryString: {QueryString}, RequestBody: {RequestBody}", context.Request.Path.Value, context.Request.QueryString.Value, requestBody);

        // If the response has already started, headers/status can no longer be changed (it would throw).
        if (context.Response.HasStarted) return;

        object response;
        if (ex is DomainException)
        {
            response = new
            {
                isSuccessful = false,
                resultCode = (int)HttpStatusCode.BadRequest,
                message = "اطلاعات وارد شده صحیح نمی باشد، لطفا مجددا تلاش نمایید."
            };
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else if (ex is ServiceException)
        {
            response = new
            {
                isSuccessful = false,
                resultCode = (int)HttpStatusCode.BadRequest,
                message = "عملیات مورد نظر با خطا رو به رو شده است، لطفا مجددا تلاش نمایید."
            };
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else
        {
            response = new
            {
                isSuccessful = false,
                resultCode = (int)HttpStatusCode.InternalServerError,
                message = "عملیات مورد نظر با خطا رو به رو شده است، لطفا مجددا تلاش نمایید."
            };
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        context.Response.ContentType = "application/Json";
        var responseBody = response.SerializeToJsonUtf8Bytes();
        await context.Response.Body.WriteAsync(responseBody);
    }
}