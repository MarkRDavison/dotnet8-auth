﻿namespace dotnet8.auth.web.Services;

public interface IApiService
{
    Task TestGet(CancellationToken cancellationToken);
    Task TestPost(CancellationToken cancellationToken);

    Task<UserInfo?> User(CancellationToken cancellationToken);
}
