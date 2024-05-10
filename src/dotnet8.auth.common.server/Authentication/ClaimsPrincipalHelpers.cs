namespace dotnet8.auth.common.server.Authentication;

public static class ClaimsPrincipalHelpers
{
    public static UserInfo? ExtractUserInfo(this ClaimsPrincipal claimsPrincipal, ClaimsSettings claimsSettings)
    {
        var idClaimValue = claimsPrincipal.Claims.FirstOrDefault(_ => _.Type == claimsSettings.IdType);

        if (!Guid.TryParse(idClaimValue?.Value, out Guid id))
        {
            return null;
        }

        var nameClaimValue = claimsPrincipal.Claims.FirstOrDefault(_ => _.Type == claimsSettings.NameType);
        var usernameClaimValue = claimsPrincipal.Claims.FirstOrDefault(_ => _.Type == claimsSettings.UsernameType);
        var emailClaimValue = claimsPrincipal.Claims.FirstOrDefault(_ => _.Type == claimsSettings.EmailType);

        return new UserInfo
        {
            Id = id,
            Name = nameClaimValue?.Value ?? string.Empty,
            Username = usernameClaimValue?.Value ?? string.Empty,
            Email = emailClaimValue?.Value ?? string.Empty
        };
    }
}
