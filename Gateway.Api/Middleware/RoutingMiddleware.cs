using Microsoft.Extensions.Options;

namespace Gateway.Api.Middleware;

public class RoutingMiddleware(RequestDelegate next, IOptions<RoutingOptions> routingOptions)
{
    private readonly Dictionary<string, string> _routes = routingOptions.Value.Routes;

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            await next(context);
            return;
        }
        
        if (context.User.Identity is { IsAuthenticated: false })
        {
            await Unauthorized(context);
            return;
        }

        var path = context.Request.Path.Value;
        if (path is null)
        {
            await NotFound(context);
            return;
        }

        var key = path.Split("/").FirstOrDefault(p => !string.IsNullOrEmpty(p));
        if (key is null || !_routes.TryGetValue(key, out var route))
        {
            await NotFound(context);
            return;
        }

        var targetUri = new Uri(route + path.Replace($"/{key}/", string.Empty));
        var requestMessage = new HttpRequestMessage
        {
            RequestUri = targetUri,
            Method = new HttpMethod(context.Request.Method),
            Content = new StreamContent(context.Request.Body)
        };

        foreach (var header in context.Request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        using var httpClient = new HttpClient();
        var responseMessage = await httpClient.SendAsync(requestMessage);

        context.Response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }

    private static async Task NotFound(HttpContext context)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Not Found");
    }
    
    private static async Task Unauthorized(HttpContext context)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
    }
}