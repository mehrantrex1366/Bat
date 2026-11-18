using System.Text;
using System.Security.Cryptography;

namespace Bat.Core;

/// <summary>
/// X25519 key agreement (.NET 11 <c>X25519DiffieHellman</c>): two parties exchange only public keys and both derive the same
/// secret key, e.g. to encrypt data between services or with a partner (with <see cref="AuthenticatedEncryption"/>)
/// without ever sending a symmetric key. The raw shared secret is passed through HKDF-SHA256 before use.
/// </summary>
/// <remarks>Keys are Base64 of the raw 32-byte X25519 keys. Supported on Windows 10+/Server 2019+ (CNG) and Linux (OpenSSL); check <see cref="IsSupported"/>.</remarks>
public static class X25519KeyAgreement
{
    private static readonly byte[] _defaultInfo = Encoding.UTF8.GetBytes("Bat.X25519KeyAgreement.v1");

    public static bool IsSupported => X25519DiffieHellman.IsSupported;

    /// <summary>Generates a key pair. Keep <c>PrivateKey</c> secret; share <c>PublicKey</c>.</summary>
    public static (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        EnsureSupported();
        using var key = X25519DiffieHellman.GenerateKey();
        return (Convert.ToBase64String(key.ExportPrivateKey()), Convert.ToBase64String(key.ExportPublicKey()));
    }

    /// <summary>
    /// Derives a <paramref name="length"/>-byte key from my private key and the peer's public key. Both sides get the same bytes.
    /// <paramref name="context"/> separates keys used for different purposes (e.g. <c>"payment-callback"</c>); both sides must use the same value.
    /// </summary>
    public static byte[] DeriveSharedKey(string myPrivateKey, string peerPublicKey, int length = 32, string context = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(myPrivateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(peerPublicKey);
        EnsureSupported();

        using var mine = X25519DiffieHellman.ImportPrivateKey(Convert.FromBase64String(myPrivateKey));
        var secret = mine.DeriveRawSecretAgreement(Convert.FromBase64String(peerPublicKey));
        try
        {
            var info = context is null ? _defaultInfo : Encoding.UTF8.GetBytes(context);
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, length, salt: null, info);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary><see cref="DeriveSharedKey"/> as Base64, ready for <see cref="AuthenticatedEncryption.Encrypt(string, string, string)"/>.</summary>
    public static string DeriveSharedKeyBase64(string myPrivateKey, string peerPublicKey, string context = null)
        => Convert.ToBase64String(DeriveSharedKey(myPrivateKey, peerPublicKey, 32, context));

    private static void EnsureSupported()
    {
        if (!X25519DiffieHellman.IsSupported)
            throw new PlatformNotSupportedException("X25519 is not supported on this operating system.");
    }
}