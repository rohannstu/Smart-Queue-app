namespace SmartQueue.Application.Interfaces;

using SmartQueue.Application.Dtos;

public interface IAuthService
{
    Task<(bool Success, string? Error, LoginResponseDto? Response)> LoginAsync(string email, string password, Guid organizationId, CancellationToken cancellationToken);
    Task<(bool Success, string? Error, RefreshTokenResponseDto? Response)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
