using System;

namespace Simular.Persist {
    /// <summary>
    /// Base form of persistence exceptions thrown by this library.
    /// </summary>
    public class PersistException : Exception {
        /// <summary>
        /// Default constructor for this exception.
        /// </summary>
        public PersistException()
            : base()
        { }

        /// <summary>
        /// Constructs this exception with a given message.
        /// </summary>
        /// <param name="message">
        /// The message for why this exception was thrown.
        /// </param>
        public PersistException(string message)
            : base(message)
        { }

        /// <summary>
        /// Constructs this exception with the given message and cause.
        /// </summary>
        /// <param name="message">
        /// The message for why this exception was thrown.
        /// </param>
        /// <param name="cause">
        /// The reason/cause for this exception having been thrown.
        /// </param>
        public PersistException(string message, Exception cause)
            : base(message, cause)
        { }
    }
}