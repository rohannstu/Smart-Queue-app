namespace SmartQueue.Application.Dtos;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    UserDto User);

public record RefreshTokenRequestDto(string RefreshToken);

public record RefreshTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role);

public record LogoutRequestDto(string RefreshToken);
