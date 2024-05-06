namespace Simular.Persist {
    /// <summary>
    /// Defines the different encryption methods that are supported by the
    /// persistence objects of this library.
    /// </summary>
    public enum EncryptionMethod {
        /// <summary>
        /// When specified, no encryption will be applied to persistence data.
        /// </summary>
        None = 0,

        /// <summary>
        /// When specified, the Advanced Encryption Standard (AES) algorithm
        /// will be applied to the persistence data.
        /// </summary>
        AES = 1
    }
}