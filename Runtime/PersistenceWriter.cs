using System;

namespace Simular.Persist {
    /// <summary>
    /// Provides functionality for writing data to the <c>Persister</c> before
    /// it is flushed to the file system.
    /// </summary>
    public sealed class PersistenceWriter {
        public Persister Persister { get; }


        /// <summary>
        /// Default constructor of a persistence writer.
        /// </summary>
        /// <param name="persister">
        /// The object which this writer relies on for cached writes and final
        /// persistence of the data being written.
        /// </param>
        public PersistenceWriter(Persister persister) {
            Persister = persister;
        }


        /// <summary>
        /// Exceptionless call to write a property, with name
        /// <paramref name="key"/>, to the JSON object which is being used to
        /// cache data before it is flushed to the file system.
        /// </summary>
        /// <param name="key">
        /// The key/name to reference the data being written by.
        /// </param>
        /// <param name="value">
        /// The value that should be written to the <c>Persister</c> under the
        /// key/name.
        /// </param>
        /// <returns>
        /// True if the data could be written to the JSON object, false
        /// otherwise.
        /// </returns>
        public bool TryWrite(string key, object value) {
            if (!Persister.IsLoaded)
                return false;

            try {
                Delete(key);
                Persister.Serialise(key, value);
                return true;
            } catch {
                return false;
            }
        }


        /// <summary>
        /// Writes a property, with name <paramref name="key"/>, to the JSON
        /// object which is being used to cache data before it is flushed to
        /// the file system.
        /// </summary>
        /// <param name="key">
        /// The key/name to reference the data being written by.
        /// </param>
        /// <param name="value">
        /// The value that should be written to the <c>Persister</c> under the
        /// key/name.
        /// </param>
        /// <exception cref="PersistException">
        /// Generic exception thrown if the data could not be serialized. Check
        /// the inner exception for more details on why it could not be
        /// serialized.
        /// </exception>
        public void Write(string key, object value) {
            if (!Persister.IsLoaded)
                throw new PersistException("Persister not loaded");
            
            try {
                Delete(key);
                Persister.Serialise(key, value);
            } catch (Exception cause) {
                throw new PersistException("Serialization Failed", cause);
            }
        }


        /// <summary>
        /// Removes an entry from the <c>Persister</c> if it exists.
        /// </summary>
        /// <param name="key">
        /// The key/name of the data mapped within the <c>Persister</c> to
        /// delete if it exists.
        /// </param>
        public void Delete(string key) {
            if (Persister.HasKey(key))
                Persister.DeleteKey(key);
        }
    }
}
