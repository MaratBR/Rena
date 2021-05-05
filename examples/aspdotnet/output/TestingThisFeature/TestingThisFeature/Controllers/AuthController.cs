using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestingThisFeature.Config;
using TestingThisFeature.Models;
using CryptSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace TestingThisFeature.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : DbController
    {
        private readonly IConfiguration configuration;

        public AuthController(AppDbContext context, IConfiguration configuration) : base(context)
        {
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            request.Username = request.Username.Trim();
            request.Email = request.Email.Trim();

            string usernameNorm = request.Username.ToLowerInvariant(),
                emailNorm = request.Email.ToLowerInvariant();
            var user = await DbContext.Users
                .Where(u => u.UsernameNormalized == usernameNorm)
                .FirstOrDefaultAsync();

            if (user != null)
                return new RegisterResponse
                {
                    Error = "Username was already taken"
                };

            user = await DbContext.Users
                .Where(u => u.EmailNormalized == emailNorm)
                .FirstOrDefaultAsync();

            if (user != null)
                return new RegisterResponse
                {
                    Error = "Email was already taken"
                };

            user = new User
            {
                Username = request.Username,
                UsernameNormalized = usernameNorm,
                Email = request.Email,
                EmailNormalized = emailNorm,
                PasswordHash = Crypter.Blowfish.Crypt(
                    Encoding.UTF8.GetBytes(request.Password),
                    Crypter.Blowfish.GenerateSalt())
            };

            DbContext.Add(user);
            await DbContext.SaveChangesAsync();
            return new RegisterResponse();
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            request.Login = request.Login.Trim();
            var loginNorm = request.Login.ToLowerInvariant();
            var user = DbContext.Users
                .FirstOrDefault(u => u.EmailNormalized == loginNorm || u.UsernameNormalized == loginNorm);

            if (user == null)
                return NotFound("User not found");

            if (!Crypter.CheckPassword(request.Password, user.PasswordHash))
                return NotFound("User not found");

            var (refresh, refreshExp) = await CreateRefreshToken(user);
            var (token, tokenExp) = CreateAccessToken(user);
            return new LoginResponse
            {
                RefreshToken = refresh,
                RefreshTokenExpiration = refreshExp,
                Token = token,
                TokenExpiration = tokenExp
            };
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("claims")]
        public ActionResult<object> GetClaims()
        {
            return new
            {
                claims = HttpContext.User.Claims.Select(c => new {c.Type, c.Value})
            };
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshTokens([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await DbContext.RefreshTokens
                .Where(tok => tok.Id == request.RefreshToken && !tok.Deactivated)
                .Include(tok => tok.User)
                .FirstOrDefaultAsync();

            if (refreshToken == null)
                return NotFound("Token not found");

            var (token, tokenExp) = CreateAccessToken(refreshToken.User);
            string newRefreshToken = null;
            DateTime? refreshTokenExp = null;

            if (request.RenewRefreshToken)
            {
                (newRefreshToken, refreshTokenExp) = await CreateRefreshToken(refreshToken.User, false);
                refreshToken.Deactivated = true;
                await DbContext.SaveChangesAsync();
            }

            return new LoginResponse
            {
                Token = token,
                TokenExpiration = tokenExp,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiration = refreshTokenExp
            };
        }

        private async Task<(string, DateTime)> CreateRefreshToken(User user, bool autoSave = true)
        {
            var p = new RNGCryptoServiceProvider();
            var exp = DateTime.UtcNow.Add(configuration.GetJwtSettings().RefreshTokenLifetime);

            var token = new RefreshToken
            {
                Id = RefreshToken.GenerateId(),
                UserAgentSha256 = UserAgentHashOrNull(),
                Ip = HttpContext.Connection.RemoteIpAddress.ToString(),
                UserId = user.Id,
                Expiration = exp
            };
            DbContext.Add(token);

            if (autoSave)
                await DbContext.SaveChangesAsync();

            return (token.Id, exp);
        }

        private (string, DateTime) CreateAccessToken(User user)
        {
            var settings = configuration.GetJwtSettings();
            var keyBytes = Encoding.UTF8.GetBytes(settings.Secret);
            var secret = settings.GetSigningKey();
            var exp = DateTime.UtcNow.Add(settings.TokenLifetime);

            var token = new JwtSecurityToken(
                settings.Issuer,
                settings.Audience,
                new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim("role", user.Role ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
                },
                expires: exp,
                signingCredentials: new SigningCredentials(secret, SecurityAlgorithms.HmacSha256)
            );
            var tokenHandler = new JwtSecurityTokenHandler();

            return (tokenHandler.WriteToken(token), exp);
        }

        public string UserAgentHashOrNull()
        {
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            if (userAgent == null)
                return null;
            var bytes = Encoding.UTF8.GetBytes(userAgent);
            var hash = SHA256.Create().ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public class RegisterRequest
        {
            [Required] public string Username { get; set; }

            [Required] public string Email { get; set; }

            [Required] public string Password { get; set; }
        }

        public class RegisterResponse
        {
            public string Error { get; set; }

            public bool Success => Error == null;
        }

        public class LoginRequest
        {
            public string Login { get; set; }

            public string Password { get; set; }
        }

        public class LoginResponse
        {
            public string RefreshToken { get; set; }

            public DateTime? RefreshTokenExpiration { get; set; }

            public string Token { get; set; }

            public DateTime TokenExpiration { get; set; }
        }

        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; }

            public bool RenewRefreshToken { get; set; }
        }
    }
}