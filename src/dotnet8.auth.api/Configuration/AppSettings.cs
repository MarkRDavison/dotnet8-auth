namespace dotnet8.auth.api.Configuration;

public class AppSettings
{
    public string SECTION => "DOTNETAUTH";

    public string AUTHORITY { get; set; } = string.Empty;
    public string AUDIENCE { get; set; } = string.Empty;
    public string CLIENT_ID { get; set; } = string.Empty;
    public string CLIENT_SECRET { get; set; } = string.Empty;
    public string SCOPES { get; set; } = string.Empty;

    public string CLAIM_NAME_ID { get; set; } = string.Empty;
    public string CLAIM_NAME_EMAIL { get; set; } = string.Empty;
    public string CLAIM_NAME_NAME { get; set; } = string.Empty;
    public string CLAIM_NAME_USERNAME { get; set; } = string.Empty;

    internal IEnumerable<string> Scopes => SCOPES.Split(" ");
}
