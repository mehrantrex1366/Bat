namespace Bat.Core;

public class AesEncryption
{
    private static string GetAlgorithmName(HashAlgorithmsTypes algorithm)
    {
        return algorithm switch
        {
            HashAlgorithmsTypes.MD5 => "MD5",
            HashAlgorithmsTypes.SHA1 => "SHA1",
            HashAlgorithmsTypes.SHA256 => "SHA256",
            _ => "MD5",
        };
    }


    public static string Encrypt(string plainText)
    {
        return AesAlgorithm.Encrypt(plainText, "!@#$%^^%$#@!", "!@#$%^", "MD5", 1, "XYZxyzAZSawsTRCE", 128);
    }

    public static string Encrypt(string plainText, HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);

        return AesAlgorithm.Encrypt(plainText, "!@#$%^^%$#@!", "!@#$%^", algorithmName, 1, "XYZxyzAZSawsTRCE", (int)keySize);
    }

    public static string Encrypt(string plainText, string encryptKey,
        HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Encrypt(plainText, encryptKey, "!@#$%^", algorithmName, 1, "XYZxyzAZSawsTRCE", (int)keySize);
    }

    public static string Encrypt(string plainText, string encryptKey,
        string salt, HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Encrypt(plainText, encryptKey, salt, algorithmName, 1, "XYZxyzAZSawsTRCE", (int)keySize);
    }

    public static string Encrypt(string plainText, string encryptKey,
        string salt, string initVector, HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Encrypt(plainText, encryptKey, salt, algorithmName, 1, initVector, (int)keySize);
    }



    public static string Decrypt(string cipherText)
    {
        return AesAlgorithm.Decrypt(cipherText.Replace(' ', '+'), "!@#$%^^%$#@!", "!@#$%^", "MD5", 1, "XYZxyzAZSawsTRCE", 128);
    }

    public static string Decrypt(string cipherText, HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Decrypt(cipherText.Replace(' ', '+'), "!@#$%^^%$#@!", "!@#$%^", algorithmName, 1, "XYZxyzAZSawsTRCE", (int)keySize);
    }

    public static string Decrypt(string cipherText, string encryptKey,
        HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Decrypt(cipherText.Replace(' ', '+'), encryptKey, "!@#$%^", algorithmName, 1, "XYZxyzAZSawsTRCE", (int)keySize);
    }

    public static string Decrypt(string cipherText, string encryptKey,
        string salt, HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Decrypt(cipherText.Replace(' ', '+'), encryptKey, salt, algorithmName, 1, "XYZxyzAZSawsTRCE", (int)keySize);
    }

    public static string Decrypt(string cipherText, string encryptKey,
        string salt, string initVector, HashAlgorithmsTypes algorithm = HashAlgorithmsTypes.MD5,
        EncryptKeySize keySize = EncryptKeySize.KeySize128)
    {
        var algorithmName = GetAlgorithmName(algorithm);
        
        return AesAlgorithm.Decrypt(cipherText.Replace(' ', '+'), encryptKey, salt, algorithmName, 1, initVector, (int)keySize);
    }
}