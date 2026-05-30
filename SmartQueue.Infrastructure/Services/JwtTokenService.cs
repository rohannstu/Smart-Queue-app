using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SmartQueue.Infrastructure.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, Guid organizationId, string email, string role, string slug);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private const int AccessTokenExpirationMinutes = 15;
    private const int RefreshTokenExpirationDays = 7;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is required in configuration.");
    }

    public string GenerateAccessToken(Guid userId, Guid organizationId, string email, string role, string slug)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("org", organizationId.ToString()),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("slug", slug),
        };

        var token = new JwtSecurityToken(
            issuer: "SmartQueue",
            audience: "SmartQueue.Api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = "SmartQueue",
                ValidateAudience = true,
                ValidAudience = "SmartQueue.Api",
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero,
            }, out var validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
