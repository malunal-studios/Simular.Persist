namespace Simular.Persist {
    /// <summary>
    /// Defines the different supported compression algorithms that can be used
    /// to compress a given data string for presistence.
    /// </summary>
    public enum CompressionMethod {
        /// <summary>
        /// Defines that no compression will be applied to the data string.
        /// </summary>
        None = 0,

        /// <summary>
        /// Defines that the GZip compression algorithm will be applied to the
        /// data string.
        /// </summary>
        GZip = 1,
    }
}