using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace TestingThisFeature.Config
{
    public class JwtSettings
    {
        [Required] public string Secret { get; set; }

        [Required] public string Issuer { get; set; }

        public string Audience { get; set; }

        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(45);

        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

        public SecurityKey GetSigningKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        }
    }

    public static class JwtSettingsExtensions
    {
        public static JwtSettings GetJwtSettings(this IConfiguration configuration)
        {
            return (JwtSettings) configuration.GetSection(nameof(JwtSettings)).Get(typeof(JwtSettings));
        }
    }
}