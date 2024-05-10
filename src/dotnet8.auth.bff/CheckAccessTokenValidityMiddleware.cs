using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using System.Net;

namespace dotnet8.auth.common.server.Middleware;
public class CheckAccessTokenValidityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<AppSettings> _options;
    private readonly HttpClient _client;
    public CheckAccessTokenValidityMiddleware(
        RequestDelegate next,
        IOptions<AppSettings> options,
        IHttpClientFactory httpClientFactory)
    {
        _next = next;
        _options = options;
        _client = httpClientFactory.CreateClient("AUTH");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var expireAt = await context.GetTokenAsync("expires_at");
        if (expireAt != null)
        {
            if (DateTime.TryParse(expireAt, null, DateTimeStyles.RoundtripKind, out var dateExpireAt))
            {
                if ((dateExpireAt - DateTime.Now).TotalMinutes < 3)
                {
                    bool tokenRefreshFailure = true;
                    var discoveryResponse = await _client.GetDiscoveryDocumentAsync(_options.Value.AUTHORITY);
                    if (!string.IsNullOrWhiteSpace(discoveryResponse.TokenEndpoint))
                    {
                        var tokenClient = new TokenClient(_client, new TokenClientOptions
                        {
                            ClientId = _options.Value.CLIENT_ID,
                            ClientSecret = _options.Value.CLIENT_SECRET,
                            Address = discoveryResponse.TokenEndpoint
                        });

                        var refreshToken = await context.GetTokenAsync("refresh_token");

                        if (!string.IsNullOrEmpty(refreshToken))
                        {
                            var tokenResult = await tokenClient.RequestRefreshTokenAsync(refreshToken);

                            if (!tokenResult.IsError &&
                                !string.IsNullOrEmpty(tokenResult.IdentityToken) &&
                                !string.IsNullOrEmpty(tokenResult.AccessToken) &&
                                !string.IsNullOrEmpty(tokenResult.RefreshToken))
                            {
                                var tokens = new List<AuthenticationToken>
                                {
                                    new AuthenticationToken {
                                        Name = OpenIdConnectParameterNames.IdToken,
                                        Value = tokenResult.IdentityToken
                                    },
                                    new AuthenticationToken
                                    {
                                        Name = OpenIdConnectParameterNames.AccessToken,
                                        Value =  tokenResult.AccessToken
                                    },
                                    new AuthenticationToken
                                    {
                                        Name = OpenIdConnectParameterNames.RefreshToken,
                                        Value = tokenResult.RefreshToken
                                    }
                                };
                                var expiresAt = DateTime.Now + TimeSpan.FromSeconds(tokenResult.ExpiresIn);
                                tokens.Add(new AuthenticationToken
                                {
                                    Name = "expires_at",
                                    Value = expiresAt.ToString("o", CultureInfo.InvariantCulture)
                                });
                                var info = await context.AuthenticateAsync("Cookies");

                                if (info != null &&
                                    info.Properties != null &&
                                    info.Principal != null)
                                {
                                    info.Properties.StoreTokens(tokens);
                                    await context.SignInAsync("Cookies", info.Principal, info.Properties);

                                    Console.WriteLine("Refreshed access token automatically");
                                    tokenRefreshFailure = false;
                                }
                            }
                        }
                    }

                    if (tokenRefreshFailure)
                    {
                        await context.SignOutAsync("Cookies");
                        await context.SignOutAsync("oidc");

                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        return;
                    }
                }
            }
        }

        var accessToken = await context.GetTokenAsync("access_token");

        context.Request.Headers.Authorization = $"Bearer {accessToken}";

        await _next.Invoke(context);
    }
}
