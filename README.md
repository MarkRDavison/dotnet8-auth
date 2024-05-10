# dotnet8-auth

Basic method of implementing the BFF pattern with Auth as simply as possible.

Blazor WASM frontend, logs in to bff using oidc (keycloak e.g.) stores auth data in HttpOnly cookie.  Bff proxies requests at /api/* using YARP to api, who then authenticates Bearer token to validate api access
