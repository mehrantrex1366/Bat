using System.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Bat.Core;

/// <summary>
/// Shared implementation for <see cref="AesAlgorithm"/> and <see cref="RijndaelAlgorithm"/>.
/// Produces byte-for-byte the same output as the previous stream-based code (AES/CBC/PKCS7,
/// key derived with PasswordDeriveBytes), so existing ciphertexts keep decrypting.
/// </summary>
/// <remarks>
/// Performance: key derivation is deterministic and was repeated on every call (these methods run
/// per value inside EF Core value converters), so derived keys are cached. Crypto objects are disposed.
/// Security note: PasswordDeriveBytes with 1 iteration + MD5 and the hard-coded defaults are weak; they are
/// kept only for compatibility with data that is already encrypted. See docs/known-issues.md.
/// </remarks>
internal static class SymmetricCrypto
{
    private static readonly ConcurrentDictionary<(string PassPhrase, string Salt, string Hash, int Iterations, int KeySize), byte[]> _keys = new();

    private static byte[] GetKey(string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, int keySize)
        => _keys.GetOrAdd((passPhrase, saltValue, hashAlgorithm, passwordIterations, keySize), static k =>
        {
#pragma warning disable SYSLIB0041 // PasswordDeriveBytes is kept for compatibility with existing ciphertexts.
            using var password = new PasswordDeriveBytes(k.PassPhrase, Encoding.ASCII.GetBytes(k.Salt), k.Hash, k.Iterations);
#pragma warning restore SYSLIB0041
            return password.GetBytes(k.KeySize / 8);
        });

    internal static string Encrypt(string plainText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
    {
        using var aes = Aes.Create();
        aes.Key = GetKey(passPhrase, saltValue, hashAlgorithm, passwordIterations, keySize);
        var cipherTextBytes = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText ?? string.Empty), Encoding.ASCII.GetBytes(initVector), PaddingMode.PKCS7);
        return Convert.ToBase64String(cipherTextBytes);
    }

    internal static string Decrypt(string cipherText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
    {
        using var aes = Aes.Create();
        aes.Key = GetKey(passPhrase, saltValue, hashAlgorithm, passwordIterations, keySize);
        var plainBytes = aes.DecryptCbc(Convert.FromBase64String(cipherText), Encoding.ASCII.GetBytes(initVector), PaddingMode.PKCS7);
        var plainText = Encoding.UTF8.GetString(plainBytes);

        // StreamReader (used previously) strips a leading UTF-8 BOM; keep that behavior.
        return plainText.Length > 0 && plainText[0] == '﻿' ? plainText[1..] : plainText;
    }
}