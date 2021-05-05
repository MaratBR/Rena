using System;
using System.Security.Cryptography;

namespace TestingThisFeature.Models
{
    public class RefreshToken
    {
        public string Id { get; set; }

        public string UserAgentSha256 { get; set; }

        public string Ip { get; set; }

        public DateTime Expiration { get; set; }

        public bool Deactivated { get; set; } = false;

        public int UserId { get; set; }

        public User User { get; set; }

        public static string GenerateId()
        {
            var id = new byte[32];
            var provider = new RNGCryptoServiceProvider();
            provider.GetBytes(id);
            return Convert.ToBase64String(id);
        }
    }
}