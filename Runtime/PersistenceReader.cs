using System;

namespace Simular.Persist {
    /// <summary>
    /// Provides functionality
    /// </summary>
    public sealed class PersistenceReader {
        public Persister Persister { get; }


        /// <summary>
        /// Default constructor of a persistence reader.
        /// </summary>
        /// <param name="persister">
        /// The object which this reader relies on for cached reads and final
        /// persistence of the data being fetched.
        /// </param>
        public PersistenceReader(Persister persister) {
            Persister = persister;
            // m_PersistenceBase.Load(); ??
        }


        /// <summary>
        /// Exceptionless call to read a property, with name <paramref name="key"/>,
        /// from the JSON object which is being used to cache data before it is
        /// flushed to the file system.
        /// </summary>
        /// <typeparam name="DataT">
        /// The type of the data to read from the JSON object.
        /// </typeparam>
        /// <param name="key">
        /// The key/name of the object within the JSON object.
        /// </param>
        /// <param name="result">
        /// An out parameter which will store the result of this operation. If
        /// the operation fails, it will be set to it's default value.
        /// </param>
        /// <returns>
        /// True if the data could be read from the JSON object, false
        /// otherwise.
        /// </returns>
        public bool TryRead<DataT>(string key, out DataT result) {
            result = default;
            if (!Persister.HasKey(key)) {
                return false;
            }

            try {
                result = Persister.Deserialize<DataT>(key);
                return true;
            } catch {
                return false;
            }
        }


        /// <summary>
        /// Reads a property, with name <paramref name="key"/>, from the JSON
        /// object which is being used to cache data before it is flushed to
        /// the file system.
        /// </summary>
        /// <typeparam name="DataT">
        /// The type of the data to read from the JSON object.
        /// </typeparam>
        /// <param name="key">
        /// The key/name of the object within the JSON object.
        /// </param>
        /// <returns>
        /// The data within the JSON object with the given key, if it is
        /// present.
        /// </returns>
        /// <exception cref="PersistKeyException">
        /// Thrown if the key which points to the data in the JSON object does
        /// not exist within it at the time of the call.
        /// </exception>
        /// <exception cref="PersistException">
        /// Generic exception thrown if the data could not be deserialized.
        /// Check the inner exception for more details on why it could not be
        /// deserialized.
        /// </exception>
        public DataT Read<DataT>(string key) {
            if (!Persister.IsLoaded)
                throw new PersistException("Persister not loaded");

            if (!Persister.HasKey(key))
                throw new PersistKeyException(key);

            try {
                return Persister.Deserialize<DataT>(key);
            } catch (Exception cause) {
                throw new PersistException("Deserialization Failed", cause);
            }
        }
    }
}