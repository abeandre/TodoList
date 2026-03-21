using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public class AuthService(IUserRepository userRepository, IConfiguration configuration, ILogger<AuthService> logger) : IAuthService
    {
        public async Task<AuthResponse?> AuthenticateAsync(AuthRequest request)
        {
            var user = await userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogWarning("Authentication failed for email: {Email}. User not found.", request.Email);
                return null;
            }

            // Verify password using PBKDF2-SHA512 (constant-time comparison)
            var saltBytes = Convert.FromBase64String(user.Salt);
            var inputHash = Rfc2898DeriveBytes.Pbkdf2(request.Password, saltBytes, 100_000, HashAlgorithmName.SHA512, 64);
            var storedHash = Convert.FromBase64String(user.HashedPassword);

            if (!CryptographicOperations.FixedTimeEquals(inputHash, storedHash))
            {
                logger.LogWarning("Authentication failed for email: {Email}. Invalid password.", request.Email);
                return null;
            }

            var jwtString = GenerateToken(user.Id, user.Name, user.Email);

            logger.LogInformation("Authentication successful for user: {Id}", user.Id);

            return new AuthResponse
            {
                Token = jwtString,
                UserName = user.Name,
                Email = user.Email
            };
        }

        public string GenerateToken(Guid userId, string name, string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key is missing or too short (minimum 32 characters). " +
                    "Set it via the JWT__Key environment variable or dotnet user-secrets.");
            var key = Encoding.UTF8.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, name),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
