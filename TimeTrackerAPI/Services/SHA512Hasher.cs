using System.Security.Cryptography;
using System.Text;

namespace TimeTrackerAPI.Services
{
    public static class SHA512Hasher
    {
        public static string HashPassword(string password)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            string hashedInputPassword = HashPassword(inputPassword);
            return hashedInputPassword == storedHash;
        }
    }
}
