namespace Simular.Persist {
    /// <summary>
    /// Defines the different persistence methods that a <c>Persister</c>
    /// supports.
    /// </summary>
    /// <remarks>
    /// These are used in conjunction with the <see cref="UnhandledEventArgs"/>
    /// to identify where an unhandled exception took place. It may not be
    /// obvious after a 'fire and forget' what method was called that could
    /// have led to the unhandled exception.
    /// </remarks>
    public enum PersistenceMethod {
        /// <summary>
        /// Defines that the <see cref="Persister.Load(bool, bool)"/> method
        /// was called.
        /// </summary>
        Load,

        /// <summary>
        /// Defines that the <see cref="Persister.LoadBackup(bool, bool)"/>
        /// method was called.
        /// </summary>
        LoadBackup,

        /// <summary>
        /// Defines that the <see cref="Persister.Flush"/> method was
        /// called.
        /// </summary>
        Flush,

        /// <summary>
        /// Defines that the <see cref="Persister.Delete"/> method was
        /// called.
        /// </summary>
        Delete,

        /// <summary>
        /// Defines that the <see cref="Persister.DeleteBackup(int)"/> was
        /// called.
        /// </summary>
        DeleteBackup,
    }
}