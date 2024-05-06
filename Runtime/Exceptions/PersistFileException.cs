using System;

namespace Simular.Persist {
    /// <summary>
    /// Persistence exception where the persisted file of a given root and
    /// profile was unable to be loaded, either because the file did not exist,
    /// or the file was empty.
    /// </summary>
    public sealed class PersistFileException : PersistException {
        /// <summary>
        /// Provides access to the root directory of the profile that the
        /// persisted file was located in when this exception was thrown.
        /// </summary>
        public string PersistenceRoot { get; set; }

        /// <summary>
        /// Provides access to the name of the profile that the persisted file
        /// was located in when this exception was thrown.
        /// </summary>
        public string PersistenceProfile { get; set; }

        /// <summary>
        /// Provides access to the file path which caused this exception to be
        /// thrown during loading of persisted data from system disk.
        /// </summary>
        public string PersistenceFile { get; set; }


        /// <summary>
        /// Creates a new exception for issues with persisted files.
        /// </summary>
        /// <param name="persistenceRoot">
        /// The root directory of the persisted file profile.
        /// </param>
        /// <param name="persistenceProfile">
        /// The name of the profile that contained the persisted file.
        /// </param>
        /// <param name="persistenceFile">
        /// The name of the persisted file on system disk that caused this
        /// exception to be thrown.
        /// </param>
        /// <param name="cause">
        /// A secondary exception which explains in greater detail why this
        /// exception was thrown.
        /// </param>
        public PersistFileException(
            string persistenceRoot,
            string persistenceProfile,
            string persistenceFile,
            Exception cause = null
        ) : base("Could not load persisted file", cause) {
            PersistenceRoot = persistenceRoot;
            PersistenceProfile = persistenceProfile;
            PersistenceFile = persistenceFile; 
        }
    }
}