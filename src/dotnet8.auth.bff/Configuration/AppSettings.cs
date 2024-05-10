﻿namespace dotnet8.auth.bff.Configuration;

public class AppSettings
{
    public string SECTION => "DOTNETAUTH";

    public string AUTHORITY { get; set; } = string.Empty;
    public string CLIENT_ID { get; set; } = string.Empty;
    public string CLIENT_SECRET { get; set; } = string.Empty;
    public string SCOPES { get; set; } = string.Empty;

    internal IEnumerable<string> Scopes => SCOPES.Split(" ");
}
