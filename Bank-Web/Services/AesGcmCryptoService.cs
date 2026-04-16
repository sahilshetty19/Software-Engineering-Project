using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Bank.Web.Services;

public sealed class AesGcmCryptoService
{
    private readonly byte[] _key;
    private readonly byte[] _aad;

    public AesGcmCryptoService(IOptions<CryptoOptions> opt)
    {
        var o = opt.Value;

        if (string.IsNullOrWhiteSpace(o.AesKeyB64))
            throw new InvalidOperationException("Crypto:AesKeyB64 is missing.");

        _key = Convert.FromBase64String(o.AesKeyB64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Crypto:AesKeyB64 must decode to 32 bytes (AES-256).");

        _aad = Encoding.UTF8.GetBytes(o.Aad ?? "");
    }

    public byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag, _aad);

        var outBytes = new byte[12 + 16 + cipher.Length];
        Buffer.BlockCopy(nonce, 0, outBytes, 0, 12);
        Buffer.BlockCopy(tag, 0, outBytes, 12, 16);
        Buffer.BlockCopy(cipher, 0, outBytes, 28, cipher.Length);
        return outBytes;
    }

    public byte[] Decrypt(byte[] blob)
    {
        if (blob == null || blob.Length < 28)
            throw new CryptographicException("Invalid encrypted payload.");

        var nonce = new byte[12];
        var tag = new byte[16];
        var cipherLen = blob.Length - 28;
        var cipher = new byte[cipherLen];

        Buffer.BlockCopy(blob, 0, nonce, 0, 12);
        Buffer.BlockCopy(blob, 12, tag, 0, 16);
        Buffer.BlockCopy(blob, 28, cipher, 0, cipherLen);

        var plain = new byte[cipherLen];
        using var aes = new AesGcm(_key);
        aes.Decrypt(nonce, cipher, tag, plain, _aad);
        return plain;
    }
}