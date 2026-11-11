using System.Security.Cryptography;
using System.Text;

namespace GPA.Helpers
{
    public static class PaystackHelper
    {
        public static bool VerifySignature(string payload, string secret, string signature)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return hashString == signature;
        }
    }
}
