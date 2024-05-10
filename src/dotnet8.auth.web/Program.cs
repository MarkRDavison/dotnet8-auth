using dotnet8.auth.web;
using dotnet8.auth.web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var services = builder.Services;


builder.RootComponents.Add<App>("#app");

services.AddHttpClient(AuthDefaults.AuthorizedClientName, client =>
{
    client.BaseAddress = new Uri("https://localhost:40000/");
})
            .AddHttpMessageHandler(_ => new CookieHandler());

services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("default"));
services.AddScoped<IApiService, ApiService>();

await builder.Build().RunAsync();