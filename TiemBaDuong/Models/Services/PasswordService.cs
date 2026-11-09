using System;
using System.Security.Cryptography;
using System.Text;

namespace TiemBaDuong.Models.Services
{
    /// <summary>
    /// Băm mật khẩu bằng PBKDF2-HMAC-SHA256 (tự cài đặt bằng HMACSHA256 để không phụ thuộc
    /// overload của .NET Framework 4.7.2). Định dạng lưu: PBKDF2-SHA256$iterations$salt$hash.
    /// Chuỗi này khoảng 80 ký tự, vừa cột NVARCHAR(255) sau migration G1.
    /// Mật khẩu cũ dạng plaintext (nếu có) sẽ không verify được và phải reset (xem DATABASE_GAPS G2).
    /// </summary>
    public static class PasswordService
    {
        private const string Prefix = "PBKDF2-SHA256";
        public const int Iterations = 600000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            byte[] key = Derive(password, salt, Iterations, KeySize);
            return string.Format("{0}${1}${2}${3}",
                Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;

            string[] parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != Prefix) return false;

            int iterations;
            if (!int.TryParse(parts[1], out iterations) || iterations < 1) return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual = Derive(password, salt, iterations, expected.Length);
            return FixedTimeEquals(actual, expected);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations, int keySize)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            const int hLen = 32;
            int blocks = (keySize + hLen - 1) / hLen;
            byte[] output = new byte[blocks * hLen];

            using (var hmac = new HMACSHA256(passwordBytes))
            {
                for (int block = 1; block <= blocks; block++)
                {
                    byte[] saltBlock = new byte[salt.Length + 4];
                    Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length);
                    saltBlock[salt.Length] = (byte)(block >> 24);
                    saltBlock[salt.Length + 1] = (byte)(block >> 16);
                    saltBlock[salt.Length + 2] = (byte)(block >> 8);
                    saltBlock[salt.Length + 3] = (byte)block;

                    byte[] u = hmac.ComputeHash(saltBlock);
                    byte[] t = (byte[])u.Clone();
                    for (int i = 1; i < iterations; i++)
                    {
                        u = hmac.ComputeHash(u);
                        for (int j = 0; j < t.Length; j++)
                        {
                            t[j] ^= u[j];
                        }
                    }
                    Buffer.BlockCopy(t, 0, output, (block - 1) * hLen, hLen);
                }
            }

            byte[] result = new byte[keySize];
            Buffer.BlockCopy(output, 0, result, 0, keySize);
            return result;
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
