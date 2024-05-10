using dotnet8.auth.common.server.Authentication;
using System.Net;

namespace dotnet8.auth.api.Authentication;

public class PopulateUserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ClaimsSettings _claimsSettings;

    public PopulateUserContextMiddleware(
        RequestDelegate next,
        IOptions<AppSettings> appSettingsOptions)
    {
        _next = next;
        _claimsSettings = new()
        {
            IdType = appSettingsOptions.Value.CLAIM_NAME_ID,
            NameType = appSettingsOptions.Value.CLAIM_NAME_NAME,
            UsernameType = appSettingsOptions.Value.CLAIM_NAME_USERNAME,
            EmailType = appSettingsOptions.Value.CLAIM_NAME_EMAIL
        };
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated ?? false)
        {
            var userInfo = context.User.ExtractUserInfo(_claimsSettings);

            if (userInfo == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Could not extract id claim");
                return;
            }

            var userContext = context.RequestServices.GetRequiredService<IUserContextService>();

            userContext.User = userInfo;
        }

        await _next(context);
    }
}
