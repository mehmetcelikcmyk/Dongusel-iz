using System.Security.Cryptography;
using System.Text;

namespace DonguselIz.Web.Services
{
    public interface ITokenService
    {
        string GenerateRememberToken(string email, int daysValid = 30);
        string? ValidateRememberToken(string token);
    }

    public class TokenService : ITokenService
    {
        private static readonly byte[] SecretKey = Encoding.UTF8.GetBytes("Gaziantep_OSB_Dongusel_Iz_Secure_Key_2026_Winter_Defense_Token");

        public string GenerateRememberToken(string email, int daysValid = 30)
        {
            var expiry = DateTimeOffset.UtcNow.AddDays(daysValid).ToUnixTimeSeconds();
            var payload = $"{email}|{expiry}";
            var signature = ComputeHmac(payload);
            var tokenString = $"{payload}|{signature}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenString));
        }

        public string? ValidateRememberToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var decodedBytes = Convert.FromBase64String(token);
                var tokenString = Encoding.UTF8.GetString(decodedBytes);
                var parts = tokenString.Split('|');

                if (parts.Length != 3) return null;

                var email = parts[0];
                if (!long.TryParse(parts[1], out var expiry)) return null;
                var signature = parts[2];

                var currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (currentUnix > expiry) return null;

                var payload = $"{email}|{expiry}";
                var expectedSignature = ComputeHmac(payload);

                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(signature),
                    Encoding.UTF8.GetBytes(expectedSignature)))
                {
                    return email;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static string ComputeHmac(string data)
        {
            using var hmac = new HMACSHA256(SecretKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash);
        }
    }
}
