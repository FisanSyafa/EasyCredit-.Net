namespace EasyCreditApi.Application.DTO.Auth;

public record ReqRefreshTokenDto
(
    string AccessToken,
    string RefreshToken
);
