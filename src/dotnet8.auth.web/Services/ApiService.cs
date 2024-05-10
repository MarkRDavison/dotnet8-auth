namespace dotnet8.auth.web.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient(AuthDefaults.AuthorizedClientName);
    }

    public async Task TestGet(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/api/test", cancellationToken);

        Console.WriteLine("StatusCode: {0}", response.StatusCode);
        Console.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task TestPost(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync("/api/test", null, cancellationToken);

        Console.WriteLine("StatusCode: {0}", response.StatusCode);
        Console.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<UserInfo?> User(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/auth/user", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
    }
}
