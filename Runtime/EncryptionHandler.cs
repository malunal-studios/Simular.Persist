using System;
using System.IO;
using System.Security.Cryptography;

namespace Simular.Persist {
    /// <summary>
    /// </summary>
    public static class EncryptionHandler {
        private static readonly byte[] AESSALTBYTES = {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
            0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        };


        /// <summary>
        /// Encrypts the given value using the chosen encryption method and
        /// provided encryption passphrase.
        /// </summary>
        /// <param name="method">
        /// The method which should be used to perform the encryption of the
        /// data value provided.
        /// </param>
        /// <param name="passphrase">
        /// An encryption passphrase to try and more securely protect the data
        /// which will be written out to disk.
        /// </param>
        /// <param name="value">
        /// The data string that needs to be encrypted.
        /// </param>
        /// <returns>
        /// The result of encrypting the value with the given method.
        /// </returns>
        public static string Encrypt(EncryptionMethod method, string passphrase, string value) {
            return method switch {
                EncryptionMethod.None => value,
                EncryptionMethod.AES  => AesEncrypt(passphrase, value),
                _ => throw new IndexOutOfRangeException()
            };
        }


        /// <summary>
        /// Decrypts the given value using the chosen encryption method and
        /// provided encryption passphrase.
        /// </summary>
        /// <param name="method">
        /// The method which should be used to perform the decryption of the
        /// data value provided.
        /// </param>
        /// <param name="passphrase">
        /// An encryption passphrase to try and more securely protect the data
        /// which will be written out to disk.
        /// </param>
        /// <param name="value">
        /// The data string that needs to be decrypted.
        /// </param>
        /// <returns>
        /// The result of decrypting the value with the given method.
        /// </returns>
        public static string Decrypt(EncryptionMethod method, string passphrase, string value) {
            return method switch {
                EncryptionMethod.None => value,
                EncryptionMethod.AES  => AesDecrypt(passphrase, value),
                _ => throw new IndexOutOfRangeException()
            };
        }


        /// <summary>
        /// Encypts the given value using the provided passphrase and the AES
        /// encryption algorithm.
        /// </summary>
        /// <param name="passphrase">
        /// An encryption passphrase to try and more securely protect the data
        /// which will be written out to disk.
        /// </param>
        /// <param name="value">
        /// The string that needs to be encrypted.
        /// </param>
        /// <returns>
        /// The encrypted string, or an empty string if the value itself is
        /// an empty string.
        /// </returns>
        /// <remarks>
        /// This will also Base64 encode the result before returning it, as an
        /// extra measure to protect the data being encrypted.
        /// </remarks>
        public static string AesEncrypt(string passphrase, string value) {
            if (string.IsNullOrEmpty(value))
                return value;

            using var aesEncryption = Aes.Create();
            using var rfc2898pdb = new Rfc2898DeriveBytes(passphrase, AESSALTBYTES, 4);

            aesEncryption.Key = rfc2898pdb.GetBytes(32);
            aesEncryption.IV = rfc2898pdb.GetBytes(16);

            using var encryptor = aesEncryption.CreateEncryptor();
            using var memorystream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memorystream, encryptor, CryptoStreamMode.Write);
            using var streamWriter = new StreamWriter(cryptoStream);

            streamWriter.Write(value);
            streamWriter.Flush();
            streamWriter.Close();
            return Convert.ToBase64String(memorystream.ToArray());
        }


        /// <summary>
        /// Decrypts the given value using the provided passphrase and the AES
        /// encryption algorithm.
        /// </summary>
        /// <param name="passphrase">
        /// The encryption passphrase that was used to encrypt the value that
        /// is also being provided..
        /// </param>
        /// <param name="value">
        /// The string that needs to be decrypted.
        /// </param>
        /// <returns>
        /// The decrypted string, or an empty string if the value itself is
        /// an empty string.
        /// </returns>
        /// <remarks>
        /// This expects that the string is a Base64 encoded string when it is
        /// passed to this function, as the <see cref="Encrypt(string,string)"/>
        /// method will convert the result to Base64 before returning it.
        /// </remarks> 
        public static string AesDecrypt(string passphrase, string value) {
            if (string.IsNullOrEmpty(value))
                return value;
            
            using var aesEncryption = Aes.Create();
            using var rfc2898pdb = new Rfc2898DeriveBytes(passphrase, AESSALTBYTES, 4);

            aesEncryption.Key = rfc2898pdb.GetBytes(32);
            aesEncryption.IV = rfc2898pdb.GetBytes(16);

            var bytes = Convert.FromBase64String(value);
            using var decryptor = aesEncryption.CreateDecryptor();
            using var memoryStream = new MemoryStream(bytes);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);

            var result = streamReader.ReadToEnd();
            streamReader.Close();
            return result;
        }
    }
}