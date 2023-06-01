using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace ProgrammingProjectApplication.Tools
{
    public static class Security
    {
        private static readonly int iterations = 10000;
        private static readonly string hash = "xrYTsJpuHivMKt2pZcLoN5rHB3KKXAglYge0pGcgWhKxI1LW45YCNOZtIDDSKn+uRryU+1JprzTi3iCvXVr3j9/7T05Qkj+v1S1QmDmMWlswS/ZZW5JN5hcVnppWbVDBkzw52XmjuoPCdQJk7qVBF0GirkuUR2utI8vPQQhmJ/qWNFBqIqyNBxkJYx0O4D9/D1LvI2xPDiMLAbMzGDlQfLkU+JFHTC4O7xcp2lPI1yMzE29p13VrdxmMQzIJxrueu5X3fudJ/BYeIYKdl2N/2cdttvfkRgeJhM6ZgPDdTiW9xwm3NgEuyatbDCPwBux/0MCF2r4kWg7oZt+dXAYU4A==";
        private static readonly string salt = "7sGAXRn5vkcqmPz2/+FeZuzEh+aeu5+uFE8pZRnWdciPfL/SWqQNM4v8L6eIL82QmHxVGghiyt5cPQtCn0WNSQ==";
        public static (string Hash, string Salt) GenerateSaltedHash(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(64);
            var salt = Convert.ToBase64String(saltBytes);

            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA512);
            var hashPassword = Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(256));

            return (hashPassword, salt);
        }

        public static void UpdatePassword(string pass)
        {
            var x = GenerateSaltedHash(pass);
            DebugHelper.WriteMessage("Hash");
            DebugHelper.WriteMessage(x.Hash);
            DebugHelper.WriteMessage("---------------------------------------------------");
            DebugHelper.WriteMessage("Salt");
            DebugHelper.WriteMessage(x.Salt);
            Console.WriteLine("x");
        }

        public static bool VerifyPassword(string enteredPassword)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(enteredPassword, saltBytes, iterations, HashAlgorithmName.SHA512);
            bool isVerified = Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(256)) == hash;

            return isVerified;
        }
    }
}
