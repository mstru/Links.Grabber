using System;
using System.Security.Cryptography;

namespace Test.Client
{
    public class SecurityUtilities
    {
        static byte[] entropy = System.Text.Encoding.Unicode.GetBytes("9aaf2717-441f-4b5e-9740-6ce7fa7a9eb5");

        public static string EncryptString(string input, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(input), entropy, scope);
            return Convert.ToBase64String(encryptedData);
        }

        public static string DecryptString(string encryptedData, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            try
            {
                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData), entropy, scope);
                return System.Text.Encoding.Unicode.GetString(decryptedData);
            }
            catch
            {
                return "";
            }
        }
    }
}
