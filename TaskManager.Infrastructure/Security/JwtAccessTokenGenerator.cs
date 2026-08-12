using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Abstractions.Security;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Security;

public sealed class JwtAccessTokenGenerator
    : IAccessTokenGenerator
{
    private const int MinimumSigningKeySizeInBytes = 32;

    private readonly JwtOptions _options;
    private readonly JsonWebTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;

    public JwtAccessTokenGenerator(
        IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;

        var signingKeyBytes =
            GetSigningKeyBytes(_options.SigningKey);

        var securityKey =
            new SymmetricSecurityKey(signingKeyBytes);

        _signingCredentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        _tokenHandler = new JsonWebTokenHandler();
    }

    public AccessToken Generate(
        User user,
        DateTimeOffset issuedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (issuedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Token issue time must be in UTC.",
                nameof(issuedAtUtc));
        }

        var expiresAtUtc =
            issuedAtUtc.AddMinutes(
                _options.AccessTokenLifetimeMinutes);

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,

                IssuedAt = issuedAtUtc.UtcDateTime,
                NotBefore = issuedAtUtc.UtcDateTime,
                Expires = expiresAtUtc.UtcDateTime,

                SigningCredentials = _signingCredentials,

                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] =
                        user.Id.ToString(),

                    [JwtRegisteredClaimNames.Email] =
                        user.Email,

                    ["display_name"] =
                        user.DisplayName,

                    [JwtRegisteredClaimNames.Jti] =
                        Guid.NewGuid().ToString("N")
                }
            };

        var tokenValue =
            _tokenHandler.CreateToken(tokenDescriptor);

        return new AccessToken(
            Value: tokenValue,
            ExpiresAtUtc: expiresAtUtc);
    }

    private static byte[] GetSigningKeyBytes(
        string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured.");
        }

        byte[] signingKeyBytes;

        try
        {
            signingKeyBytes =
                Convert.FromBase64String(signingKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT signing key must be a valid Base64 string.",
                exception);
        }

        if (signingKeyBytes.Length <
            MinimumSigningKeySizeInBytes)
        {
            throw new InvalidOperationException(
                $"JWT signing key must contain at least " +
                $"{MinimumSigningKeySizeInBytes} bytes.");
        }

        return signingKeyBytes;
    }
}