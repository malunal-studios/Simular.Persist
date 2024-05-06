namespace Simular.Persist {
    /// <summary>
    /// A persistence exception where the 'key' of a given data storage
    /// structure was not present upon request.
    /// </summary>
    public sealed class PersistKeyException : PersistException {
        /// <summary>
        /// Gets the key that caused the exception to be thrown.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Default constructor for this exception.
        /// </summary>
        /// <param name="key">
        /// The key which caused this exception to be thrown.
        /// </param>
        public PersistKeyException(string key)
            : base($"Key \"{key}\" does not exist")
        { Key = key; }
    }
}