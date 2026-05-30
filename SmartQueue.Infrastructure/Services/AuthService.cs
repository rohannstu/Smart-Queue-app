using Microsoft.EntityFrameworkCore;
using SmartQueue.Application.Dtos;
using SmartQueue.Application.Interfaces;
using SmartQueue.Domain.Entities;
using SmartQueue.Infrastructure.Data.Context;

namespace SmartQueue.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly SmartQueueDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(SmartQueueDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<(bool Success, string? Error, LoginResponseDto? Response)> LoginAsync(string email, string password, Guid organizationId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.OrganizationId == organizationId && !u.IsDeleted, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return (false, "Invalid email or password.", null);
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return (false, "Invalid email or password.", null);
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, organizationId, user.Email, user.Role, string.Empty);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role);

        var response = new LoginResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(7),
            User: userDto);

        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, RefreshTokenResponseDto? Response)> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsDeleted, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return (false, "Invalid or expired refresh token.", null);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId && !u.IsDeleted, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return (false, "User not found or inactive.", null);
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, storedToken.OrganizationId, user.Email, user.Role, string.Empty);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        storedToken.ReplacedByToken = newRefreshToken;
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            OrganizationId = storedToken.OrganizationId,
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
        };

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new RefreshTokenResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(7));

        return (true, null, response);
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsDeleted, cancellationToken);

        if (storedToken is null)
        {
            return false;
        }

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
