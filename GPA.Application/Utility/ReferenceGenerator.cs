using System.Security.Cryptography;

namespace GPA.Application.Utility
{
    public static class ReferenceGenerator
    {
        private const string Prefix = "UNIPA";

        public static string Generate()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

            Span<byte> bytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(bytes);

            var random = Convert.ToHexString(bytes);

            return $"{Prefix}-{timestamp}-{random}";
        }
    }
}
