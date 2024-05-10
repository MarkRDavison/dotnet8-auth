var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

var services = builder.Services;

services
    .AddScoped<IApiService, ApiService>()
    .AddHttpClient(AuthDefaults.AuthorizedClientName, client =>
    {
        client.BaseAddress = new Uri("https://localhost:40000/");
    })
    .AddHttpMessageHandler(_ => new CookieHandler());

await builder.Build().RunAsync();