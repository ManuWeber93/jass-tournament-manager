using JassTournamentManager.Application.Auth;
using JassTournamentManager.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JassTournamentManager.Infrastructure.Auth
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly JwtOptions _jwtOptions;

        public TokenGenerator(IOptions<JwtOptions> jwtOptions)
        {
            _tokenHandler = new JwtSecurityTokenHandler();
            _jwtOptions = jwtOptions.Value;
        }

        public AccessTokenSecret GenerateAccessTokenSecret(User user)
        {
            List<Claim> claims = CreateAccessTokenClaims(user);

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresAt.UtcDateTime,
                signingCredentials: signingCredentials);

            var tokenString = _tokenHandler.WriteToken(token);
            return new AccessTokenSecret(tokenString, expiresAt);
        }

        public RefreshTokenSecret GenerateRefreshTokenSecret()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            string tokenString = Base64UrlEncoder.Encode(bytes);

            string tokenHash = HashRefreshToken(tokenString);

            var expiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

            return new RefreshTokenSecret(tokenString, tokenHash, expiresAt);
        }

        public string HashRefreshToken(string refreshToken)
        {
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }

        private static List<Claim> CreateAccessTokenClaims(User user)
        {
            return
                [
                    new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(JwtRegisteredClaimNames.Iat,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64),
                    new(ClaimTypes.Email, user.Email ?? string.Empty),
                    new("is_sys_admin", user.IsSysAdmin ? "true" : "false")
                ];
        }
    }
}
