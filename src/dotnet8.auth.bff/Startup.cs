namespace dotnet8.auth.bff;

public class Startup
{
    public IConfiguration Configuration { get; }

    public AppSettings AppSettings { get; set; } = new();

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        Configuration.GetSection(AppSettings.SECTION).Bind(AppSettings);

        services
            .AddCors();

        services
            .AddSingleton(Options.Create(AppSettings))
            .AddSingleton(AppSettings);

        services
            .AddAuthentication(_ =>
            {
                _.DefaultScheme = "Cookies";
                _.DefaultChallengeScheme = "oidc";
                _.DefaultSignOutScheme = "oidc";
            })
            .AddOpenIdConnect("oidc", _ =>
            {
                _.Authority = AppSettings.AUTHORITY;
                _.ClientId = AppSettings.CLIENT_ID;
                _.ClientSecret = AppSettings.CLIENT_SECRET;

                _.TokenValidationParameters = new()
                {
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(5)
                };

                _.ResponseType = OpenIdConnectResponseType.Code;

                // Go to the user info endpoint to retrieve additional claims after creating an identity from the id_token
                _.GetClaimsFromUserInfoEndpoint = true;
                // Store access and refresh tokens in the authentication cookie
                _.SaveTokens = true;
                _.Scope.Clear();

                foreach (var scope in AppSettings.Scopes)
                {
                    _.Scope.Add(scope);
                }

                _.Events = new()
                {
                    OnMessageReceived = async (MessageReceivedContext ctx) =>
                    {
                        Console.WriteLine("OnMessageReceived");
                        await Task.CompletedTask;
                    },
                    OnTokenResponseReceived = async (TokenResponseReceivedContext ctx) =>
                    {
                        Console.WriteLine("OnTokenResponseReceived");
                        await Task.CompletedTask;
                    },
                    OnAccessDenied = async (AccessDeniedContext ctx) =>
                    {
                        Console.WriteLine("OnAccessDenied");
                        await Task.CompletedTask;
                    },
                    OnUserInformationReceived = async ctx =>
                    {
                        await Task.CompletedTask;
                    },
                    OnTokenValidated = async (TokenValidatedContext ctx) =>
                    {
                        Console.WriteLine("OnTokenValidated: {0}", ctx.SecurityToken.RawPayload);
                        await Task.CompletedTask;
                    }
                };
            })
            .AddCookie("Cookies", _ =>
            {
                _.ExpireTimeSpan = TimeSpan.FromHours(8);
                _.SlidingExpiration = false;
                _.Cookie.Name = "__MySPA";
                // When the value is Strict the cookie will only be sent along with "same-site" requests.
                _.Cookie.SameSite = SameSiteMode.Strict; // TODO: Prod mode
                _.LogoutPath = "/logout-complete";
                _.LoginPath = "/login";
            });

        services
            .AddAuthorization();

        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();

        services.AddHttpClient("AUTH");
        services.AddReverseProxy();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder => builder
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .SetIsOriginAllowed(_ => true) // TODO: Config driven
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .AllowAnyHeader());

        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        app.UseRouting();
        app.UseAuthentication();
        app.UseMiddleware<CheckAccessTokenValidityMiddleware>();
        app.UseAuthorization();
        app.UseHttpsRedirection();

        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var transformer = HttpTransformer.Default;
        var requestConfig = new ForwarderRequestConfig
        {
            ActivityTimeout = TimeSpan.FromSeconds(100)
        };
        var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15),
        });


        app.UseEndpoints(endpoints =>
        {
            endpoints
                .MapGet("/bff/test", async (HttpContext context) =>
                {
                    var accessToken = await context.GetTokenAsync("access_token");
                    var refreshToken = await context.GetTokenAsync("refresh_token");

                    Console.WriteLine("Access Token: \n{0}", accessToken);
                    Console.WriteLine("Refresh Token: \n{0}", refreshToken);
                    return Results.Ok("Success from bff :)");
                })
                .RequireAuthorization();

            endpoints
                .Map("/api/{*rest}", async (HttpContext context, [FromServices] IHttpForwarder forwarder) =>
                {
                    var error = await forwarder
                    .SendAsync(
                        context,
                        "https://localhost:50000",
                        httpClient,
                        requestConfig,
                        transformer);
                    // Check if the operation was successful
                    if (error != ForwarderError.None)
                    {
                        var errorFeature = context.GetForwarderErrorFeature();
                        var exception = errorFeature?.Exception;

                        if (exception != null)
                        {
                            Console.WriteLine(exception.Message);
                            Console.WriteLine(exception.StackTrace);
                        }
                    }
                })
                .RequireAuthorization();

            endpoints.MapGet("/auth/user", (HttpContext context) =>
            {
                if (context.User?.Identity?.IsAuthenticated ?? false)
                {
                    var name = context.User.Claims.FirstOrDefault(_ => _.Type == "name")?.Value;

                    return Results.Ok(name);
                }

                return Results.Unauthorized();
            }).AllowAnonymous();

            endpoints.MapGet("/login", async (HttpContext context, [FromQuery(Name = "returnUrl")] string? returnUrl) =>
            {
                var prop = new AuthenticationProperties
                {
                    RedirectUri = returnUrl ?? "https://localhost:8080/login-complete"
                };

                await context.ChallengeAsync(prop);
            }).AllowAnonymous();

            endpoints.MapGet("/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync("Cookies");
                var prop = new AuthenticationProperties
                {
                    RedirectUri = "https://localhost:8080/logout-complete"
                };
                await context.SignOutAsync("oidc", prop);
            });
        });
    }
}