using Microsoft.Extensions.Configuration;
using System.IO;
using System.Security.Cryptography;

namespace CMCSPrototype.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            _key = Convert.FromBase64String(configuration["EncryptionSettings:Key"]);
            _iv = Convert.FromBase64String(configuration["EncryptionSettings:IV"]);
        }

        public byte[] Encrypt(Stream inputStream)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        inputStream.CopyTo(cryptoStream);
                    }
                    return memoryStream.ToArray();
                }
            }
        }

        public Stream Decrypt(byte[] encryptedData)
        {
            var memoryStream = new MemoryStream();
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var cryptoStream = new CryptoStream(new MemoryStream(encryptedData), decryptor, CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(memoryStream);
                }
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
