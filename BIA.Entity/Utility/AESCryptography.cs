using BIA.Entity.Collections;
using Serilog;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.Utility
{
    public class AESCryptography
    {
        public static string Encrypt(string plainText)
        {
            byte[] encrypted;
            string aes_key = SettingsValues.GetAESKey();
            string aes_iv = SettingsValues.GetAESIv();

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(aes_key);
                aes.IV = Convert.FromBase64String(aes_iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ICryptoTransform enc = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }

                        encrypted = ms.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encryptedText)
        {
            string decrypted = string.Empty;
            byte[] cipher = Convert.FromBase64String(encryptedText);
            string aes_key = SettingsValues.GetAESKey();
            string aes_iv = SettingsValues.GetAESIv();
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(aes_key);
                    aes.IV = Convert.FromBase64String(aes_iv);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using ICryptoTransform dec = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(cipher))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, dec, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                decrypted = sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExMessage");
                if (string.Equals(ex.Message, "Padding is invalid and cannot be removed."))
                {
                    decrypted = "The value is not encrypted with AES or inavilid token!";
                }
                else
                {
                    decrypted = "AES information not found!";
                }
                return decrypted;
            }

            return decrypted;
        }


        public static byte[] Decrypts(byte[] cipherBytes)
        {
            byte[] decrypted;
            string aes_key = SettingsValues.GetAESKey();
            string aes_iv = SettingsValues.GetAESIv();

            using (Aes aes = Aes.Create())
            {
                // Set AES Key and IV
                aes.Key = Convert.FromBase64String(aes_key);
                aes.IV = Convert.FromBase64String(aes_iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Create AES Decryptor
                using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream plainTextStream = new MemoryStream())
                        {
                            // Read the decrypted bytes into the plainTextStream
                            cs.CopyTo(plainTextStream);
                            decrypted = plainTextStream.ToArray();
                        }
                    }
                }
            }

            // Return the decrypted byte array
            return decrypted;
        }

        public static byte[]? DecryptFingerData(byte[] encryptedData)
        {
            try
            {
                // 1. AES key (loaded dynamically from configuration)
                string aesKeyBase64 = SettingsValues.GetAESKey();
                byte[] keyBytes = Convert.FromBase64String(aesKeyBase64);

                // 2. Extract IV (first 12 bytes)
                byte[] iv = encryptedData[..12];

                // 3. Extract ciphertext + tag (GCM uses 16-byte tag, typically appended)
                byte[] cipherTextWithTag = encryptedData[12..];

                // 4. Separate cipher and tag (last 16 bytes are tag)
                int tagLength = 16;
                byte[] cipherText = cipherTextWithTag[..^tagLength];
                byte[] tag = cipherTextWithTag[^tagLength..];

                byte[] plainData = new byte[cipherText.Length];

                using var aesGcm = new AesGcm(keyBytes);
                aesGcm.Decrypt(iv, cipherText, tag, plainData);

                return plainData;
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine("Decryption failed: Invalid authentication tag.");
                Console.WriteLine(ex);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                Console.WriteLine(ex);
                return null;
            }
        }

        public static string DecryptAES_FP(byte[] cipher)
        {
            string decrypted = string.Empty;
            //byte[] cipher = Convert.FromBase64String(encryptedText);
            string aes_key = SettingsValues.GetAESKey();
            string aes_iv = SettingsValues.GetAESIv();
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(aes_key);
                    aes.IV = Convert.FromBase64String(aes_iv);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using ICryptoTransform dec = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(cipher))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, dec, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                decrypted = sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                decrypted = "InvalidSessionToken";
                return decrypted;
            }

            return decrypted;
        }       

        public static bool Verify(string encryptedString, string plainText)
        {
            // Check arguments.
            if (encryptedString == null || encryptedString.Length <= 0)
                throw new ArgumentNullException("encryptedString");

            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            var a = Decrypt(encryptedString);
            return string.Equals(Decrypt(encryptedString), plainText);
        }

        public static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            byte[] encrypted;

            using (Aes aesAlg = Aes.Create()) 
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
                    {
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }
        public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            string plaintext;

            using (Aes aesAlg = Aes.Create()) // cross-platform safe
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                using ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
                {
                    plaintext = srDecrypt.ReadToEnd();
                }
            }

            return plaintext;
        }
    }
}
