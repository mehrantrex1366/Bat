using System.Text;
using System.Security.Cryptography;

namespace Bat.Core;

/// <summary>
/// Modern, authenticated encryption for <b>new</b> data: AES-256-GCM with a random key per message (envelope encryption),
/// the message key wrapped with your master key using AES Key Wrap (RFC 3394, .NET 11 <c>Aes.EncryptKeyWrap</c>).
/// Unlike <see cref="Encryption"/> / <see cref="AesEncryption"/> (kept unchanged for existing data), the output is different every time
/// (random key and nonce), tampering is detected, and the key comes from your configuration, not from the library.
/// </summary>
/// <remarks>
/// Format (Base64): <c>[version 0x01][wrapped key 40 B][nonce 12 B][tag 16 B][ciphertext]</c>.
/// The master key must be 16, 24 or 32 random bytes (<see cref="GenerateKey"/>); store it in a secret store, not in code.
/// Optional <c>associatedData</c> (e.g. the record id) is authenticated but not stored: decryption must pass the same value.
/// Rotating the master key only needs the 40-byte wrapped key of each value to be re-wrapped, not the data re-encrypted.
/// Not searchable or comparable: the same plaintext never produces the same ciphertext. Use <see cref="HashGenerator"/>-style lookups for that.
/// </remarks>
public static class AuthenticatedEncryption
{
    private const byte Version1 = 0x01;
    private const int DataKeySize = 32;
    private const int WrappedKeySize = DataKeySize + 8;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int HeaderSize = 1 + WrappedKeySize + NonceSize + TagSize;

    /// <summary>Generates a random 256-bit master key as Base64 (put it in your secret store / configuration).</summary>
    public static string GenerateKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(DataKeySize));

    public static string Encrypt(string plainText, string base64MasterKey, string associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plainText), DecodeKey(base64MasterKey), ToBytes(associatedData)));
    }

    public static string Decrypt(string cipherText, string base64MasterKey, string associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(cipherText), DecodeKey(base64MasterKey), ToBytes(associatedData)));
    }

    /// <summary>Returns <c>false</c> instead of throwing when the value was tampered with or the key / associated data is wrong.</summary>
    public static bool TryDecrypt(string cipherText, string base64MasterKey, out string plainText, string associatedData = null)
    {
        try
        {
            plainText = Decrypt(cipherText, base64MasterKey, associatedData);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            plainText = null;
            return false;
        }
    }

    public static byte[] Encrypt(byte[] plainBytes, byte[] masterKey, byte[] associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(plainBytes);
        ValidateMasterKey(masterKey);

        var output = new byte[HeaderSize + plainBytes.Length];
        var span = output.AsSpan();
        span[0] = Version1;
        var wrappedKey = span.Slice(1, WrappedKeySize);
        var nonce = span.Slice(1 + WrappedKeySize, NonceSize);
        var tag = span.Slice(1 + WrappedKeySize + NonceSize, TagSize);
        var cipher = span[HeaderSize..];

        Span<byte> dataKey = stackalloc byte[DataKeySize];
        try
        {
            RandomNumberGenerator.Fill(dataKey);
            RandomNumberGenerator.Fill(nonce);

            using (var kek = Aes.Create())
            {
                kek.Key = masterKey;
                kek.EncryptKeyWrap(dataKey, wrappedKey);
            }

            using var gcm = new AesGcm(dataKey, TagSize);
            gcm.Encrypt(nonce, plainBytes, cipher, tag, associatedData);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }

        return output;
    }

    public static byte[] Decrypt(byte[] cipherBytes, byte[] masterKey, byte[] associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(cipherBytes);
        ValidateMasterKey(masterKey);
        if (cipherBytes.Length < HeaderSize || cipherBytes[0] != Version1)
            throw new CryptographicException("The value is not in the AuthenticatedEncryption format (version 1).");

        var span = cipherBytes.AsSpan();
        var wrappedKey = span.Slice(1, WrappedKeySize);
        var nonce = span.Slice(1 + WrappedKeySize, NonceSize);
        var tag = span.Slice(1 + WrappedKeySize + NonceSize, TagSize);
        var cipher = span[HeaderSize..];
        var plain = new byte[cipher.Length];

        Span<byte> dataKey = stackalloc byte[DataKeySize];
        try
        {
            using (var kek = Aes.Create())
            {
                kek.Key = masterKey;
                if (kek.DecryptKeyWrap(wrappedKey, dataKey) != DataKeySize)
                    throw new CryptographicException("Invalid wrapped key.");
            }

            using var gcm = new AesGcm(dataKey, TagSize);
            gcm.Decrypt(nonce, cipher, tag, plain, associatedData);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }

        return plain;
    }

    private static byte[] DecodeKey(string base64MasterKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64MasterKey);
        return Convert.FromBase64String(base64MasterKey);
    }

    private static byte[] ToBytes(string value) => value is null ? null : Encoding.UTF8.GetBytes(value);

    private static void ValidateMasterKey(byte[] masterKey)
    {
        ArgumentNullException.ThrowIfNull(masterKey);
        if (masterKey.Length is not (16 or 24 or 32))
            throw new ArgumentException("The master key must be 16, 24 or 32 bytes (AES-128/192/256).", nameof(masterKey));
    }
}