using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace YoavProject
{
    internal class Encryption
    {
        public const int RSAsize = 2048;
        public const int AESsize = 256;
        public static (string privateKey, string publicKey) generateRSAkeypair()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(RSAsize);
            rsa.PersistKeyInCsp = false;
            try
            {
                string privateKey = Convert.ToBase64String(rsa.ExportCspBlob(true));
                string publicKey = Convert.ToBase64String(rsa.ExportCspBlob(false));

                return (privateKey, publicKey);
            }
            finally
            {

                rsa.Dispose();
            }

        }

        public static string generateAESkey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = AESsize;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        public static byte[] encryptRSA(byte[] message, string publicKey)
        {
            Console.WriteLine(publicKey);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.PersistKeyInCsp = false;

            try
            {
                rsa.ImportCspBlob(Convert.FromBase64String(publicKey));
                byte[] res = rsa.Encrypt(message, false);

                return res;
            }
            finally
            {
                rsa.Dispose();
            }
        }

        public static byte[] decryptRSA(byte[] message, string privateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.PersistKeyInCsp = false;

            try
            {
                rsa.ImportCspBlob(Convert.FromBase64String(privateKey));
                byte[] res = rsa.Decrypt(message, false);

                return res;
            }
            finally
            {
                rsa.Dispose();
            }
        }

        public static byte[] encryptAES(byte[] message, string stringKey)
        {
            byte[] key = Convert.FromBase64String(stringKey);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(message, 0, message.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }

            }
        }

        public static byte[] decryptAES(byte[] message, string stringKey)
        {
            byte[] key = Convert.FromBase64String(stringKey);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(message, iv, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(message, iv.Length, message.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var outputMs = new MemoryStream())
                {
                    cs.CopyTo(outputMs);  // copy decrypted bytes to outputMs
                    return outputMs.ToArray();
                }
            }
        }
    }
}
