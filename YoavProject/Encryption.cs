using System;
using System.IO;
using System.Security.Cryptography;

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
            byte[] key = Convert.FromBase64String(stringKey); //here

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

        //public static byte[] decryptAES(byte[] message, string stringKey)
        //{
        //    byte[] key = Convert.FromBase64String(stringKey);

        //    using (Aes aes = Aes.Create())
        //    {
        //        aes.Key = key;
        //        Console.WriteLine($"Key length: {key.Length} bytes");
        //        aes.Mode = CipherMode.CBC;
        //        aes.Padding = PaddingMode.PKCS7;
        //        if (message.Length < aes.BlockSize / 8)
        //            throw new ArgumentException("Encrypted message too short to contain IV.");


        //        byte[] iv = new byte[aes.BlockSize / 8];
        //        Array.Copy(message, iv, iv.Length);
        //        aes.IV = iv;

        //        using (var decryptor = aes.CreateDecryptor())
        //        using (var ms = new MemoryStream(message, iv.Length, message.Length - iv.Length))
        //        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        //        using (var outputMs = new MemoryStream())
        //        {
        //            cs.CopyTo(outputMs);  // copy decrypted bytes to outputMs
        //            return outputMs.ToArray();
        //        }
        //    }
        //}

        public static byte[] decryptAES(byte[] message, string stringKey)
        {
            Console.WriteLine($"[decryptAES] Total message length: {message.Length}");

            byte[] key = Convert.FromBase64String(stringKey);
            Console.WriteLine($"[decryptAES] AES key length: {key.Length}");

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                if (message.Length < aes.BlockSize / 8)
                {
                    Console.WriteLine("[decryptAES] Encrypted message too short for IV.");
                    throw new ArgumentException("Encrypted message too short to contain IV.");
                }

                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(message, iv, iv.Length);
                aes.IV = iv;
                Console.WriteLine("[decryptAES] IV: " + BitConverter.ToString(iv));

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(message, iv.Length, message.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var outputMs = new MemoryStream())
                {
                    try
                    {
                        cs.CopyTo(outputMs);
                        Console.WriteLine("[decryptAES] Successfully decrypted.");
                        return outputMs.ToArray();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[decryptAES] Exception during decryption: " + ex.Message);
                        throw;
                    }
                }
            }
        }

    }
}
