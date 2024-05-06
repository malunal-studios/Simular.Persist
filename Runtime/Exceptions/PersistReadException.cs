namespace Simular.Persist {
    /// <summary>
    /// A persistence exception where an attempt to read a persistence file
    /// was successful but there were no contents within the file.
    /// </summary>
    /// <remarks>
    /// This is an important exception because we treat empty files as
    /// problematic. Realistically, a persisted file should not be empty ever.
    /// If it is supposed to be empty, ask yourself why you are creating empty
    /// files for no reason. If you have a reason, the best suggestion is to
    /// either ignore this exception or not create the empty files in the first
    /// place.
    /// </remarks>
    public sealed class PersistReadException : PersistException {
        public PersistReadException() : base("No file contents") {
        }
    }
}