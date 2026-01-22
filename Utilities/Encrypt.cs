using System.Security.Cryptography;
using System.Text;

namespace IPOClient.Utilities
{
    public static class Encrypt
    {
        public static readonly string key = "b52bbfdb-2263-43d2-9bce-cd66624723cc";

        public static readonly string IV = "o6806642kbM7c518";

        public static string HexString2B64String(this string input) => Convert.ToBase64String(input.HexStringToHex());

        public static byte[] HexStringToHex(this string inputHex)
        {
            byte[] resultantArray = new byte[inputHex.Length / 2];
            for (int i = 0; i < resultantArray.Length; i++)
            {
                resultantArray[i] = Convert.ToByte(inputHex.Substring(i * 2, 2), 16);
            }
            return resultantArray;
        }

        public static string DecryptStringAESWithHax(string cipherText)
        {
            byte[] keybytes = Encoding.UTF8.GetBytes(key).Take(32).ToArray();
            byte[] iv = Encoding.UTF8.GetBytes(IV);
            return string.Format(DecryptStringFromBytes_Aes(Convert.FromBase64String(cipherText.HexString2B64String()), keybytes, iv));
        }

        public static string EncryptStringAESWithHax(string cipherText)
        {
            byte[] keybytes = Encoding.UTF8.GetBytes(key).Take(32).ToArray();
            byte[] iv = Encoding.UTF8.GetBytes(IV);
            return string.Format(BitConverter.ToString(EncryptStringToBytes_Aes(cipherText, keybytes, iv)).Replace("-", ""));
        }

        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
            {
                throw new ArgumentNullException("plainText");
            }
            if (Key == null || Key.Length == 0)
            {
                throw new ArgumentNullException("Key");
            }
            if (IV == null || IV.Length == 0)
            {
                throw new ArgumentNullException("IV");
            }
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Key;
            aesAlg.IV = IV;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            return msEncrypt.ToArray();
        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length == 0)
            {
                throw new ArgumentNullException("cipherText");
            }
            if (Key == null || Key.Length == 0)
            {
                throw new ArgumentNullException("Key");
            }
            if (IV == null || IV.Length == 0)
            {
                throw new ArgumentNullException("IV");
            }
            string plaintext = null;
            using Aes aesAlg = Aes.Create();
            aesAlg.Key = Key;
            aesAlg.IV = IV;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using MemoryStream msDecrypt = new MemoryStream(cipherText);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
    }
}
