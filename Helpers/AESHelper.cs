using System.Security.Cryptography;

public static class AESHelper
{
    public static (byte[] Key, byte[] IV) GenerateAESKeyAndIV()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;  // AES-256
        aes.GenerateKey();
        aes.GenerateIV();
        return (aes.Key, aes.IV);
    }
}
