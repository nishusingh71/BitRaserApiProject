using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// AES Encryption Helper for encrypting API responses
    /// Uses Gzip compression + AES-256-CBC encryption with PKCS7 padding
    /// Flow: PlainText → Gzip Compress (if >1KB) → AES Encrypt → Base64
    /// </summary>
    public static class EncryptionHelper
    {
        // Minimum bytes for compression to be effective (1KB)
        private const int MinCompressionThreshold = 1024;

        /// <summary>
        /// Encrypt plain text using optional Gzip compression + AES-256-CBC
        /// Compression is only applied for data larger than 1KB to avoid size inflation
        /// </summary>
        /// <param name="plainText">The plain text to encrypt</param>
        /// <param name="key">32-byte encryption key (AES-256)</param>
        /// <param name="iv">16-byte initialization vector (AES block size)</param>
        /// <returns>Base64 encoded encrypted string</returns>
        public static string Encrypt(string plainText, string key, string? iv = null)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Encryption key cannot be null or empty", nameof(key));
            }

            // Ensure key is exactly 32 bytes (256 bits) for AES-256
            byte[] keyBytes = PadOrTruncate(Encoding.UTF8.GetBytes(key), 32);

            // Generate or use provided IV (must be 16 bytes for AES)
            byte[] ivBytes;
            if (string.IsNullOrEmpty(iv))
            {
                ivBytes = GenerateRandomIV();
            }
            else
            {
                ivBytes = PadOrTruncate(Encoding.UTF8.GetBytes(iv), 16);
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] dataToEncrypt;

            // ✅ Smart Compression: Only compress if payload > 1KB
            // Small payloads actually get LARGER after compression due to overhead
            if (plainBytes.Length >= MinCompressionThreshold)
            {
                dataToEncrypt = CompressGzip(plainBytes);
            }
            else
            {
                // Skip compression for small payloads
                dataToEncrypt = plainBytes;
            }

            // ✅ AES encrypt the data
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Write IV to the beginning of the stream (needed for decryption)
            ms.Write(ivBytes, 0, ivBytes.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(dataToEncrypt, 0, dataToEncrypt.Length);
            }

            // Return Base64 encoded: [IV + Encrypted(Data)]
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Check if data was compressed (for use in response wrapper)
        /// </summary>
        public static bool ShouldCompress(int byteCount) => byteCount >= MinCompressionThreshold;

        /// <summary>
        /// Decrypt encrypted text using AES-256-CBC + Gzip decompression
        /// </summary>
        /// <param name="encryptedText">Base64 encoded encrypted string</param>
        /// <param name="key">32-byte encryption key (AES-256)</param>
        /// <returns>Decrypted plain text</returns>
        public static string Decrypt(string encryptedText, string key)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Encryption key cannot be null or empty", nameof(key));
            }

            // Ensure key is exactly 32 bytes (256 bits) for AES-256
            byte[] keyBytes = PadOrTruncate(Encoding.UTF8.GetBytes(key), 32);

            // Decode Base64 string
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            // Extract IV from the beginning (first 16 bytes)
            byte[] ivBytes = new byte[16];
            Array.Copy(encryptedBytes, 0, ivBytes, 0, 16);

            // Extract encrypted data (after IV)
            byte[] cipherBytes = new byte[encryptedBytes.Length - 16];
            Array.Copy(encryptedBytes, 16, cipherBytes, 0, cipherBytes.Length);

            // ✅ Step 1: AES decrypt
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(cipherBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msDecompressed = new MemoryStream();
            csDecrypt.CopyTo(msDecompressed);
            byte[] decryptedBytes = msDecompressed.ToArray();

            // ✅ Step 2: Gzip decompress
            byte[] decompressedBytes = DecompressGzip(decryptedBytes);

            return Encoding.UTF8.GetString(decompressedBytes);
        }

        /// <summary>
        /// Compress data using Gzip
        /// </summary>
        private static byte[] CompressGzip(byte[] data)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return outputStream.ToArray();
        }

        /// <summary>
        /// Decompress Gzip data
        /// </summary>
        private static byte[] DecompressGzip(byte[] compressedData)
        {
            using var inputStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        /// <summary>
        /// Generate a random 16-byte IV for AES encryption
        /// </summary>
        private static byte[] GenerateRandomIV()
        {
            byte[] iv = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(iv);
            return iv;
        }

        /// <summary>
        /// Pad or truncate byte array to specified length
        /// </summary>
        private static byte[] PadOrTruncate(byte[] data, int length)
        {
            if (data.Length == length)
            {
                return data;
            }

            byte[] result = new byte[length];

            if (data.Length > length)
            {
                Array.Copy(data, result, length);
            }
            else
            {
                Array.Copy(data, result, data.Length);
            }

            return result;
        }

        /// <summary>
        /// Validate if a string is a valid Base64 encoded string
        /// </summary>
        public static bool IsBase64String(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
