using dotnet8.auth.common.server.Authentication;

namespace dotnet8.auth.api.Authentication;

public class UserContextService : IUserContextService
{
    public UserInfo User { get; set; } = new();
}
