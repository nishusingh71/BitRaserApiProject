using System.Security.Cryptography;
using System.Text;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// AES Encryption Helper for encrypting API responses
    /// Uses AES-256-CBC encryption with PKCS7 padding
    /// </summary>
    public static class EncryptionHelper
    {
    /// <summary>
/// Encrypt plain text using AES-256-CBC
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

            // Validate key length (must be 32 bytes for AES-256)
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
                // Generate a random IV for each encryption (more secure)
                ivBytes = GenerateRandomIV();
       }
            else
       {
     ivBytes = PadOrTruncate(Encoding.UTF8.GetBytes(iv), 16);
        }

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
  using (var sw = new StreamWriter(cs))
            {
 sw.Write(plainText);
            }

    // Return Base64 encoded: [IV + Encrypted Data]
            return Convert.ToBase64String(ms.ToArray());
   }

        /// <summary>
        /// Decrypt encrypted text using AES-256-CBC
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

            using var aes = Aes.Create();
        aes.Key = keyBytes;
    aes.IV = ivBytes;
aes.Mode = CipherMode.CBC;
 aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
 
         return sr.ReadToEnd();
        }

        /// <summary>
     /// Generate a random 16-byte IV for AES encryption
        /// </summary>
        /// <returns>16-byte random IV</returns>
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
        /// <param name="data">Input byte array</param>
        /// <param name="length">Target length</param>
        /// <returns>Byte array of exact length</returns>
        private static byte[] PadOrTruncate(byte[] data, int length)
        {
    if (data.Length == length)
      {
         return data;
 }

      byte[] result = new byte[length];
  
if (data.Length > length)
            {
    // Truncate
       Array.Copy(data, result, length);
            }
else
        {
        // Pad with zeros
         Array.Copy(data, result, data.Length);
            }

         return result;
        }

  /// <summary>
   /// Validate if a string is a valid Base64 encoded string
      /// </summary>
        /// <param name="base64String">String to validate</param>
        /// <returns>True if valid Base64, false otherwise</returns>
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
