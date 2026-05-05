using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartCartPro.Business.Interfaces;
using SmartCartPro.DataAccess.Interfaces;
using SmartCartPro.Models.Common;
using SmartCartPro.Models.DTOs.Auth;
using SmartCartPro.Models.Entities;
using SmartCartPro.Models.Enums;


namespace SmartCartPro.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthService(IAuthRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }


        // ── REGISTER ───────────────────────────────────────────

        public async Task<string> RegisterAsync(RegisterRequestDto dto)
        {
            if (await _repo.EmailExistsAsync(dto.Email.ToLower().Trim()))
                throw new AppException("Email is already registered.");

            if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
                role = UserRole.Customer;

            var user = new User
            {
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12),
                FirstName = dto.FirstName.ToLower().Trim(),
                LastName = dto.LastName.ToLower().Trim(),
                Role = role
            };

            await _repo.CreateUserAsync(user);
            return "Registration successful. Please login.";
        }

        // ── LOGIN ──────────────────────────────────────────────
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _repo.GetUserByEmailAsync(dto.Email.ToLower().Trim())
              ?? throw new AppException("Invalid email or password.", 401);

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new AppException("Invalid email or password.", 401);

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(
                int.Parse(_config["JwtSettings:RefreshTokenExpiry"] ?? "7"));

            await _repo.SaveRefreshTokenAsync(user.UserId, refreshToken, refreshExpiry);

            await _repo.UpdateLastLoginAsync(user.UserId);

            return BuildResponse(user, accessToken, refreshToken);
        }

        // ── REFRESH ────────────────────────────────────────────
        public async Task<LoginResponseDto> RefreshAsync (string refreshToken)
        {
            var tokenRecord = await _repo.GetRefreshTokenAsync(refreshToken)
                ?? throw new AppException("Refresh token expired. Please login again.", 401);

            if(tokenRecord.Expiry < DateTime.UtcNow)
            {
                await _repo.RevokeRefreshTokenAsync(refreshToken);
                throw new AppException("Refresh token expired. Please login again.", 401);
            }

            var user = await _repo.GetUserByIdAsync(tokenRecord.UserId)
                ?? throw new AppException("User not found.", 401);

            var newAccess = GenerateAccessToken(user);
            var newRefresh = GenerateRefreshToken();
            var expiry = DateTime.UtcNow.AddDays(int.Parse(_config["JwtSettings:RefreshTokenExpiry"] ?? "7"));

            await _repo.RevokeRefreshTokenAsync(refreshToken);
            await _repo.SaveRefreshTokenAsync(user.UserId, newRefresh, expiry);

            return BuildResponse(user, newAccess, newRefresh);

        }

        // ── LOGOUT ─────────────────────────────────────────────

        public async Task LogoutAsync(string refreshToken)
        {
            await _repo.RevokeRefreshTokenAsync(refreshToken);
        }


        // ── PRIVATE HELPERS ────────────────────────────────────
        private string GenerateAccessToken(User user)
        {
            var secret = _config["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = int.Parse(_config["JwtSettings:AccessTokenExpiry"] ?? "15");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", $"{user.FirstName} {user.LastName}")
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }


        private LoginResponseDto BuildResponse(User user, string access, string refresh) =>
            new()
            {
                AccessToken = access,
                RefreshToken = refresh,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["JwtSettings:AccessTokenExpiry"] ?? "15")),
                User = new UserInfoDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Role = user.Role.ToString()
                }
            };
    }

}
