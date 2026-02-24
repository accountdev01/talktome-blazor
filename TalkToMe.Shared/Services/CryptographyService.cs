using Microsoft.Extensions.Configuration;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TalkToMe.Shared.IService;

namespace TalkToMe.Shared.Services
{
    public class CryptographyService : ICryptographyService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public CryptographyService(IConfiguration configuration)
        {
            var keyStr = configuration["CryptoSettings:Key"] ?? throw new Exception("Missing Key");
            var ivStr = configuration["CryptoSettings:IV"] ?? throw new Exception("Missing IV");

            try
            {
                _key = Convert.FromBase64String(keyStr);
                _iv = Convert.FromBase64String(ivStr);

                if (_key.Length != 32)
                    throw new InvalidOperationException($"Key size mismatch! Expected 32 bytes, but got {_key.Length} bytes.");
                if (_iv.Length != 16)
                    throw new InvalidOperationException($"IV size mismatch! Expected 16 bytes, but got {_iv.Length} bytes.");
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Key or IV is not a valid Base64 string. Please check your configuration.");
            }
        }

        public void GenerateKeys()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;

            aes.GenerateKey();
            aes.GenerateIV();

            Console.WriteLine($"Generated Key (Base64): {Convert.ToBase64String(aes.Key)}");
            Console.WriteLine($"Generated IV (Base64): {Convert.ToBase64String(aes.IV)}");
        }

        public string Protect(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;

            try
            {
                using var aes = Aes.Create();
                ValidateCryptoParams(aes);

                aes.Key = _key;
                aes.IV = _iv;

                using var encryptor = aes.CreateEncryptor();
                using var ms = new MemoryStream();

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs, Encoding.UTF8))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                LogError("Protect", ex.Message);
                throw new CryptographicException("Error during data protection process.", ex);
            }
        }

        public string Unprotect(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return string.Empty;

            try
            {
                var buffer = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                ValidateCryptoParams(aes);

                aes.Key = _key;
                aes.IV = _iv;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch (FormatException)
            {
                LogError("Unprotect", "Invalid Base64 string provided for decryption.");
                return string.Empty;
            }
            catch (CryptographicException ex)
            {
                LogError("Unprotect", $"Data tampering or key mismatch detected: {ex.Message}");
                return "DECRYPTION_FAILED";
            }
            catch (Exception ex)
            {

                return string.Empty;
            }
        }

        private void ValidateCryptoParams(Aes aes)
        {
            if (_key == null || _key.Length != (aes.KeySize / 8))
                throw new InvalidOperationException($"Invalid Key size. Expected {aes.KeySize / 8} bytes.");

            if (_iv == null || _iv.Length != (aes.BlockSize / 8))
                throw new InvalidOperationException($"Invalid IV size. Expected {aes.BlockSize / 8} bytes.");
        }

        private void LogError(string fn, string msg) => LoggerHelper.WriteLog($"CryptographyService -> {fn}", msg);
    }
}
