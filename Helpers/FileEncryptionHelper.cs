using System.Security.Cryptography;
using System.Text;

namespace SecureFileSharingApp.Helpers
{
    public static class FileEncryptionHelper
    {
        // AES-256 key (32 bytes) and IV (16 bytes)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 chars
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 16 chars

        public static string GetSecureUploadPath(string fileName)
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, fileName);
        }
        public static async Task EncryptAndSaveFileAsync(Stream inputStream, string outputPath)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using var fileStream = new FileStream(outputPath, FileMode.Create);
            using var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            inputStream.Position = 0;
            await inputStream.CopyToAsync(cryptoStream);
            await cryptoStream.FlushAsync();
        }

        public static async Task DecryptFileAsync(string encryptedFilePath, Stream outputStream)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using var fileStream = new FileStream(encryptedFilePath, FileMode.Open);
            using var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

            await cryptoStream.CopyToAsync(outputStream);
        }
    }
}
