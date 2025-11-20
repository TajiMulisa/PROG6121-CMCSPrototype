using System.IO;

namespace CMCSPrototype.Services
{
    public interface IEncryptionService
    {
        byte[] Encrypt(Stream inputStream);
        Stream Decrypt(byte[] encryptedData);
    }
}
