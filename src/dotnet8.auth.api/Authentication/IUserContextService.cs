namespace dotnet8.auth.api.Authentication;

public interface IUserContextService
{
    UserInfo User { get; set; }
}
