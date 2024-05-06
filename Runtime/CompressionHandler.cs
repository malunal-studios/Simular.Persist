using System;
using System.IO;
using System.IO.Compression;

namespace Simular.Persist {
    /// <summary>
    /// </summary>
    public static class CompressionHandler {
        /// <summary>
        /// Compresses the given value using the chosen compression method.
        /// </summary>
        /// <param name="method">
        /// The method which should be used to perform the compression of the
        /// data value provided.
        /// </param>
        /// <param name="value">
        /// The data string that needs to be compressed.
        /// </param>
        /// <returns>
        /// The result of compressing the value with the given method.
        /// </returns>
        public static string Compress(CompressionMethod method, string value) {
            return method switch {
                CompressionMethod.None => value,
                CompressionMethod.GZip => GZipCompress(value),
                _ => throw new IndexOutOfRangeException()
            };
        }


        /// <summary>
        /// Decompress the given value using the chosen compression method.
        /// </summary>
        /// <param name="method">
        /// The method which should be used to perform the decompression of the
        /// data value provided.
        /// </param>
        /// <param name="value">
        /// The data string that needs to be decompressed.
        /// </param>
        /// <returns>
        /// The result of decompressing the value with the given method.
        /// </returns>
        public static string Decompress(CompressionMethod method, string value) {
            return method switch {
                CompressionMethod.None => value,
                CompressionMethod.GZip => GZipDecompress(value),
                _ => throw new IndexOutOfRangeException()
            };
        }


        /// <summary>
        /// Compresses the given value using the GZip compression algorithm.
        /// </summary>
        /// <param name="value">
        /// The string that needs to be compressed.
        /// </param>
        /// <returns>
        /// The compressed string, or an empty string if the value itself is
        /// an empty string.
        /// </returns>
        /// <remarks>
        /// This will also Base64 encode the result before returning it.
        /// </remarks>
        public static string GZipCompress(string value) {
            if (string.IsNullOrEmpty(value))
                return value;
            
            using var memoryStream = new MemoryStream();
            using var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress);
            using var streamWriter = new StreamWriter(compressionStream);

            streamWriter.Write(value);
            streamWriter.Flush();
            streamWriter.Close();
            return Convert.ToBase64String(memoryStream.ToArray());
        }


        /// <summary>
        /// Decompresses the given value using the GZip compression algorithm.
        /// </summary>
        /// <param name="value">
        /// The string that needs to be decompressed.
        /// </param>
        /// <returns>
        /// The decompressed string, or an empty string if the value itself is
        /// an empty string.
        /// </returns>
        /// <remarks>
        /// This expects that the string is a Base64 encoded string when it is
        /// passed to this function, as the <see cref="Compress(string)"/>
        /// method will convert the result to Base64 before returning it.
        /// </remarks>
        public static string GZipDecompress(string value) {
            if (string.IsNullOrEmpty(value))
                return value;

            var bytes = Convert.FromBase64String(value);
            using var memoryStream = new MemoryStream(bytes);
            using var compressionStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(compressionStream);

            var result = streamReader.ReadToEnd();
            streamReader.Close();
            return result;
        }
    }
}