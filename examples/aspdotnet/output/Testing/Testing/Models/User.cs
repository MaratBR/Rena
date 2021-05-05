using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Testing.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string UsernameNormalized { get; set; }

        public string Email { get; set; }

        public string EmailNormalized { get; set; }

        [JsonIgnore] public string PasswordHash { get; set; }

        public string Role { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Timestamp] [ConcurrencyCheck] public byte[] ConsurrencyToken { get; set; }
    }
}