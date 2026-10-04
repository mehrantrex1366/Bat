namespace Bat.Core;

// RijndaelManaged with a 128-bit block (the only block size .NET supports) is AES, so the shared AES
// implementation produces identical output.
public class RijndaelAlgorithm
{
    public static string Encrypt(string plainText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
        => SymmetricCrypto.Encrypt(plainText, passPhrase, saltValue, hashAlgorithm, passwordIterations, initVector, keySize);

    public static string Decrypt(string cipherText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
        => SymmetricCrypto.Decrypt(cipherText, passPhrase, saltValue, hashAlgorithm, passwordIterations, initVector, keySize);
}
