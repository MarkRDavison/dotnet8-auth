﻿namespace dotnet8.auth.api;

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
            .AddSingleton(AppSettings)
            .AddScoped<IUserContextService, UserContextService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = AppSettings.AUTHORITY,
                    ValidateAudience = !string.IsNullOrEmpty(AppSettings.AUDIENCE),
                    ValidAudience = AppSettings.AUDIENCE,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = false,
                    ClockSkew = TimeSpan.Zero,
                    SignatureValidator = (token, _) => new JsonWebToken(token),
                    RequireExpirationTime = true,
                };
            });

        services
            .AddAuthorization()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app
            .UseCors(builder => builder
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .SetIsOriginAllowed(_ => true) // TODO: Config driven
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader());

        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<PopulateUserContextMiddleware>();

        app.UseHttpsRedirection();

        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints
                .MapGet("/api/test", (HttpContext context, IUserContextService userContext) =>
                {
                    if (context.User?.Identity?.IsAuthenticated ?? false)
                    {
                        return Results.Ok($"GET Success from api :) thanks {userContext.User.Name}");
                    }
                    return Results.Unauthorized();
                })
                .RequireAuthorization();

            endpoints
                .MapPost("/api/test", (HttpContext context, IUserContextService userContext) =>
                {
                    if (context.User?.Identity?.IsAuthenticated ?? false)
                    {
                        return Results.Ok($"POST Success from api :) thanks {userContext.User.Name}");
                    }
                    return Results.Unauthorized();
                })
                .RequireAuthorization();
        });
    }
}