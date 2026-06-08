namespace SafeEye.Application.Auth.Dtos;

public record AuthResultDto(string AccessToken, string RefreshToken, UserInfoDto User);
public record UserInfoDto(Guid Id, string Name, string Email);