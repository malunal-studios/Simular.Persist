using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions;


namespace Simular.Persist {
    /// <summary>
    /// Represents an object which is designed to be persisted.
    /// </summary>
    public sealed class Persister {
        /// <summary>
        /// Additional arguments which make up the basis of the
        /// <see cref="LoadEventArgs"/> and the <see cref="FlushEventArgs"/>.
        /// </summary>
        public class PersistEventArgs : EventArgs {
            /// <summary>
            /// Provides a problem that may have occurred and caused the
            /// loading process to terminate early.
            /// </summary>
            /// <remarks>
            /// This is intended to provide more details to the potential
            /// failure that took place. If no problem occurred while trying to
            /// load the persistence file, this will be empty. All other cases,
            /// this will contain a valid exception with a potential cause.
            /// </remarks>
            public PersistException Problem { get; internal set; }

            /// <summary>
            /// Provides the number of backups are currently on the system disk.
            /// </summary>
            /// <remarks>
            /// This is additional information which may be useful to know when
            /// attempting to handle a problem if on occurs. By providing it
            /// here, you don't need to perform extra processing to get it from
            /// the <c>Persister</c>.
            /// </remarks>
            public int BackupCount { get; internal set; }

            /// <summary>
            /// Provides the index of the backup that was loaded fromm the
            /// system disk.
            /// </summary>
            /// <remarks>
            /// This is additional information which may be useful to know when
            /// attempting to handle a problem if on occurs. By providing it
            /// here, you don't need to perform extra processing to get it from
            /// the <c>Persister</c>.
            /// </remarks>
            public int BackupIndex { get; internal set; }

            /// <summary>
            /// Whether the file loaded from the system disk was in fact, a
            /// backup file.
            /// </summary>
            /// <remarks>
            /// This is additional information to help distinguish if the
            /// persistence operation was performed on the main persistence
            /// data file, or a backup of that file.
            /// </remarks>
            public bool Backup => BackupIndex != -1;
        }


        /// <summary>
        /// Additional arguments provided when the <see cref="OnLoad"/> event
        /// is triggered by any given <c>Persister</c>.
        /// </summary>
        public class LoadEventArgs : PersistEventArgs {
            /// <summary>
            /// Whether or not the file did not have contents at the time of
            /// reading the file from the file system.
            /// </summary>
            /// <remarks>
            /// This will be set regardless the value of the <c>mayBeEmpty</c>
            /// parameter in bot the <see cref="Load()"/> and
            /// <see cref="LoadBackup()"/> functions. It is provided for
            /// additional context to the properties of the load operation that
            /// took place. This means that it can be used instead of the
            /// <see cref="Problem"/> property to quickly identify issues.
            /// </remarks>
            public bool EmptyFile { get; internal set; }

            /// <summary>
            /// Whether or not the file did not exist on the file system at the
            /// time of loading it.
            /// </summary>
            /// <remarks>
            /// This will be set regardless the value of the <c>mayNotExist</c>
            /// parameter in both the <see cref="Load()"/> and
            /// <see cref="LoadBackup()"/> functions. It is provided for
            /// additional context to the properties of the load operation that
            /// took place. This means that it can be used instead of the
            /// <see cref="Problem"/> property to quickly identify issues.
            /// </remarks>
            public bool NotFound { get; internal set; }
        }


        /// <summary>
        /// Additional arguments provided when the <see cref="OnFlush"/> event
        /// is triggered by any given <c>Persister</c>.
        /// </summary>
        public class FlushEventArgs : PersistEventArgs {
        }


        /// <summary>
        /// Additional arguments provided when the <see cref="OnDelete"/> event
        /// is triggered by any given <c>Persister</c>.
        /// </summary>
        public class DeleteEventArgs : PersistEventArgs {
        }


        /// <summary>
        /// Additional arguments provided when the <see cref="OnUnhandled"/>
        /// event is triggered by any given <c>Persister</c>.
        /// </summary>
        public class UnhandledEventArgs : EventArgs {
            /// <summary>
            /// The exception that was thrown.
            /// </summary>
            public Exception Cause { get; set; }

            /// <summary>
            /// The method that the exception was thrown in.
            /// </summary>
            public PersistenceMethod Method { get; set; }
        }


        /// <summary>
        /// <para>
        /// Defines an interface for listening to major events on a
        /// <c>Persister</c>.
        /// </para>
        /// <para>
        /// The functions provided in this interface are designed to be used
        /// and assigned to one of <see cref="Persister.OnSave"/>,
        /// <see cref="Persister.OnLoad"/>, or <see cref="Persister.OnFlush"/>.
        /// By providing this design, we can facilitate most needs of the
        /// calling software, as it will likely need to be informed when the
        /// data is being saved, loaded, and flushed. For example, listening to
        /// each of these events you could implement a system which allows the
        /// indication of saving that is happening in the background of the
        /// running application, and presenting an error to the user that the
        /// data could not be persisted, as well as providing options for
        /// loading backups from disk at the same time.
        /// </para>
        /// </summary>
        public interface IListener {
            /// <summary>
            /// Called when a <c>Persister</c> is requested to flush data to
            /// the system disk, however the flush will not have executed yet,
            /// giving systems a chance to finalize the data they wish to
            /// persist.
            /// </summary>
            /// <param name="persister">
            /// The <c>Persister</c> which is saving data that will be flushed
            /// to system disk. 
            /// </param>
            /// <param name="args">
            /// Generic event arguments, will almost always be empty.
            /// </param>
            /// <remarks>
            /// Handling this event is useful for systems where data needs to
            /// be loaded into the <c>Persister</c> cache before it's fully
            /// flushed.
            /// </remarks>
            void OnSave(object persister, EventArgs args) { }

            /// <summary>
            /// Called when a <c>Persister</c> loads data from the system disk,
            /// this includes backups.
            /// </summary>
            /// <param name="persister">
            /// The <c>Persister</c> which loaded the data from system disk.
            /// </param>
            /// <param name="args">
            /// The arguments passed from the <see cref="Load()"/> thread
            /// internal function when it completes. Contains information on
            /// how the function performed and what it performed on.
            /// </param>
            /// <remarks>
            /// Handling this event is useful for systems where data is needed
            /// immmediately after loading. Data may be loaded multiple times
            /// if different profiles are chosen or a backup is loaded.
            /// </remarks>
            void OnLoad(object persister, LoadEventArgs args) { }

            /// <summary>
            /// Called when a <c>Persister</c> flushes data to the system disk.
            /// </summary>
            /// <param name="persister">
            /// The <c>Persister</c> which flushed the data to system disk.
            /// </param>
            /// <param name="args">
            /// The arguments passed from the <see cref="Flush()"/> thread
            /// internal function when it completes. Contains information on
            /// how the function performed and what it performed on.
            /// </param>
            /// <remarks>
            /// Handling this event is useful for systems where data persistence
            /// needs to be validated, that is, whether the data was actually
            /// persisted or not is integral to the function of the application
            /// and it's success or failure needs proper reporting.
            /// </remarks>
            void OnFlush(object persister, FlushEventArgs args) { }

            /// <summary>
            /// Called when a <c>Persister</c> deletes data from the system
            /// disk, this includes backups.
            /// </summary>
            /// <param name="persister">
            /// The <c>Persister</c> which deleted the data from system disk.
            /// </param>
            /// <param name="args">
            /// The arguments passed from the <see cref="Delete()"/> thread
            /// internal function when it completes. Contains information on
            /// how the function performed and what it performed on.
            /// </param>
            /// <remarks>
            /// Handling this event is useful for system which need to detect
            /// when a file is deleted by <c>Persister</c>. As it may need to
            /// recreate the file afterwards or report it to the user.
            /// </remarks>
            void OnDelete(object persister, DeleteEventArgs args) { }

            /// <summary>
            /// Called when the <c>Persister</c> experiences an unhandled
            /// exception, likely from the execution of a callback which threw
            /// an exception internally.
            /// </summary>
            /// <param name="persister">
            /// The <c>Persister</c> which caught the unhandled exception after
            /// executing some callback.
            /// </param>
            /// <param name="cause">
            /// The exception that was thrown and caught by the <c>Persister</c>.
            /// </param>
            void OnUnhandled(object persister, UnhandledEventArgs cause) { }
        }


        /// <summary>
        /// Defines the settings for a given <c>Persister</c>.
        /// </summary>
        [Serializable]
        public class Settings {
            /// <summary>
            /// The path of the root folder for data persistence when
            /// flushing data to the file system.
            /// </summary>
            public string persistenceRoot = "saves";

            /// <summary>
            /// The name of the profile folder for data persistence when
            /// flushing data to the file system.
            /// </summary>
            public string persistenceProfile = "default";

            /// <summary>
            /// The name of the file for data persistence when flushing the
            /// data to the file system.
            /// </summary>
            public string persistenceFile = "save-game";

            /// <summary>
            /// A phrase, passcode, or password to use when encrypting and
            /// decrypting persistence data.
            /// </summary>
            /// <remarks>
            /// This acts like a salt, making it harder for an attacker to
            /// modify the persited data.
            /// </remarks>
            public string encryptionPhrase = string.Empty;

            /// <summary>
            /// The maximum number of backups to create for any given
            /// persistence data file.
            /// </summary>
            public int maxBackups = 0;

            /// <summary>
            /// Defines the compression method for the <c>Persister</c> that
            /// will receive these settings.
            /// </summary>
            public CompressionMethod compression = CompressionMethod.None;

            /// <summary>
            /// Defines the encryption method for the <c>Persister</c> that
            /// will receive these settings.
            /// </summary>
            public EncryptionMethod encryption = EncryptionMethod.None;
        }


        private static readonly JsonSerializerSettings
            M_SERIALIZER_SETTINGS = new() {
                Converters = new List<JsonConverter>() {

                }
            };

        private static readonly JsonSerializer
            M_SERIALIZER = JsonSerializer.Create(M_SERIALIZER_SETTINGS);


        private Settings m_Settings;
        private FileHandler m_FileHandler;
        private JObject m_Object;


        /// <summary>
        /// An event which calls listeners who are subscribed to data being
        /// saved into a given <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// This event is called from a separate thread for performance reasons.
        /// If you need to access Unity related assets, you won't be able to
        /// safely. The likelihood is that Unity won't allow you or report a
        /// warning in the logs that you're doing so. You may need to delegate
        /// the operations you need performed to another object or design them
        /// in such a way that they can be detected from the main thread and
        /// executed when Unity calls the object to update again.
        /// </remarks>
        public static event EventHandler OnSave;


        /// <summary>
        /// An event which calls listeners who are subscribed to data being
        /// loaded into a given <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// This event is called from a separate thread for performance reasons.
        /// If you need to access Unity related assets, you won't be able to
        /// safely. The likelihood is that Unity won't allow you or report a
        /// warning in the logs that you're doing so. You may need to delegate
        /// the operations you need performed to another object or design them
        /// in such a way that they can be detected from the main thread and
        /// executed when Unity calls the object to update again.
        /// </remarks>
        public static event EventHandler<LoadEventArgs> OnLoad;


        /// <summary>
        /// An event which calls listeners who are subscribed to data being
        /// flushed to disk from a <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// This event is called from a separate thread for performance reasons.
        /// If you need to access Unity related assets, you won't be able to
        /// safely. The likelihood is that Unity won't allow you or report a
        /// warning in the logs that you're doing so. You may need to delegate
        /// the operations you need performed to another object or design them
        /// in such a way that they can be detected from the main thread and
        /// executed when Unity calls the object to update again.
        /// </remarks>
        public static event EventHandler<FlushEventArgs> OnFlush;


        /// <summary>
        /// An event which calls listeners who are subscribed to data being
        /// deleted from disk by a <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// This event is called from a separate thread for performance reasons.
        /// If you need to access Unity related assets, you won't be able to
        /// safely. The likelihood is that Unity won't allow you or report a
        /// warning in the logs that you're doing so. You may need to delegate
        /// the operations you need performed to another object or design them
        /// in such a way that they can be detected from the main thread and
        /// executed when Unity calls the object to update again.
        /// </remarks>
        public static event EventHandler<DeleteEventArgs> OnDelete;


        /// <summary>
        /// An event which calls listeners who are subscribed to handling
        /// exceptions that may not have been caught because it happened within
        /// another event.
        /// </summary>
        /// <remarks>
        /// Like the other events this one is called from a separate thread,
        /// however this is only coincidental, since it's called if one of the
        /// others throw. If a listener subscribed to this event throws when it
        /// is called, there is no recovery option!
        /// </remarks>
        public static event EventHandler<UnhandledEventArgs> OnUnhandled;


        /// <summary>
        /// The persistence root provided when creating this <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The well known disk location of the data that will be persisted by
        /// this <c>Persister</c>. Each persistence root may have one or more
        /// persistence profiles which each contain the same data structures
        /// but different values.
        /// </para>
        /// <para>
        /// This function has a minor side effect! It will recreate a handler
        /// class internally to redirect file operations to the appropriate
        /// path and file.
        /// </para>
        /// </remarks>
        public string PersistenceRoot {
            get => m_Settings.persistenceRoot;
            set {
                Assert.IsFalse(string.IsNullOrEmpty(value));
                m_Settings.persistenceRoot = value;
                m_FileHandler = new(
                    m_Settings.persistenceRoot,
                    m_Settings.persistenceProfile,
                    m_Settings.persistenceFile,
                    m_Settings.maxBackups
                );
            }
        }


        /// <summary>
        /// The persistence profile under the persistence root provided when
        /// creating this <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The persistence root is like a namespace for different persistence
        /// profiles. Each root may contain different persisted data, but each
        /// profile will contain the same structure with different values.
        /// </para>
        /// <para>
        /// This function has a minor side effect! It will recreate a handler
        /// class internally to redirect file operations to the appropriate
        /// path and file.
        /// </para>
        /// </remarks>
        public string PersistenceProfile {
            get => m_Settings.persistenceProfile;
            set {
                Assert.IsFalse(string.IsNullOrEmpty(value));
                m_Settings.persistenceProfile = value;
                m_FileHandler = new(
                    m_Settings.persistenceRoot,
                    m_Settings.persistenceProfile,
                    m_Settings.persistenceFile,
                    m_Settings.maxBackups
                );
            }
        }


        /// <summary>
        /// The persistence file that data will be persisted to when flush is
        /// called on this <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The persistence file is the name of the file where the persistence
        /// data will be written to when a call to <see cref="Flush()"/> or
        /// <see cref="TryFlush()"/> is made.
        /// </para>
        /// <para>
        /// This function has a minor side effect! It will recreate a handler
        /// class internally to redirect file operations to the appropriate
        /// path and file.
        /// </para>
        /// </remarks>
        public string PersistenceFile {
            get => m_Settings.persistenceFile;
            set {
                Assert.IsFalse(string.IsNullOrEmpty(value));
                m_Settings.persistenceFile = value;
                m_FileHandler = new(
                    m_Settings.persistenceRoot,
                    m_Settings.persistenceProfile,
                    m_Settings.persistenceFile,
                    m_Settings.maxBackups
                );
            }
        }


        /// <summary>
        /// The fully constructed path to where the data will be persisted by
        /// this <c>Persister</c>.
        /// </summary>
        /// <remarks>
        /// The well known fully qualified file system path of the data that
        /// will be persisted by this <c>Persister</c>. It is the combination
        /// of the <see cref="PersistenceRoot"/> and the
        /// <see cref="PersistenceProfile"/>.
        /// </remarks>
        public string PersistencePath => m_FileHandler.PersistencePath;


        /// <summary>
        /// The method this <c>Persister</c> uses to compress and decompress
        /// the data.
        /// </summary>
        public CompressionMethod Compression => m_Settings.compression;


        /// <summary>
        /// The method this <c>Persister</c> uses to encrypt and decrypt the
        /// data.
        /// </summary>
        public EncryptionMethod Encryption => m_Settings.encryption;


        /// <summary>
        /// Default constructor for this class.
        /// </summary>
        /// <param name="settings">
        /// The necessary settings for a <c>Persister</c> to function.
        /// </param>
        public Persister(Settings settings) {
            Assert.IsFalse(string.IsNullOrEmpty(settings.persistenceRoot));
            Assert.IsFalse(string.IsNullOrEmpty(settings.persistenceFile));
            if (settings.encryption != EncryptionMethod.None)
                Assert.IsFalse(string.IsNullOrEmpty(settings.encryptionPhrase));

            m_Object = new JObject();
            m_Settings = settings;
            m_FileHandler = new FileHandler(
                settings.persistenceRoot,
                settings.persistenceProfile,
                settings.persistenceFile,
                settings.maxBackups
            );
        }


        /// <summary>
        /// Adds a new JSON converter to this <c>Persister</c> if it doesn't
        /// already have it.
        /// </summary>
        /// <param name="converter">
        /// The converter to register.
        /// </param>
        public static void RegisterConverter(JsonConverter converter) {
            if (!M_SERIALIZER.Converters.Any(c => c.GetType() == converter.GetType()))
                M_SERIALIZER.Converters.Add(converter);
        }


        /// <summary>
        /// Loads the most recent persisted file from the system disk for the
        /// configured root and profile.
        /// </summary>
        /// <param name="mayBeEmpty">
        /// <para>
        /// Indicates to the <c>Persister</c> that the file contents may not be
        /// present, the file is empty. It bypasses the typical exception check
        /// to prevent unwanted errors when it's known that the file may have no
        /// contents.
        /// </para>
        /// <para>
        /// An exception for the file not existing will still be thrown as it's
        /// expected that one exist if your going to read it. To bypass this as
        /// well, set the <paramref name="mayNotExist"/> parameter to true.
        /// </para>
        /// <para>
        /// When recording this problem, a <see cref="PersistFileExcpetion"/>
        /// will be provided in the <see cref="LoadEventArgs.Problem"/> property
        /// with no cause.
        /// </para>
        /// </param>
        /// <param name="mayNotExist">
        /// <para>
        /// Indicates to the <c>Persister</c> that the file may not be present
        /// in the file system at the time of reading. It bypasses the typical
        /// exception check to prevent unwanted errors when it's known that
        /// the file may not be present entirely.
        /// </para>
        /// <para>
        /// When recording this problem, a <see cref="PersistFileException"/>
        /// will be provided in the <see cref="LoadEventArgs.Problem"/> property
        /// with a <see cref="FileNotFoundException"/>.
        /// </para>
        /// </param>
        public void Load(bool mayBeEmpty = false, bool mayNotExist = false) {
            Assert.IsNotNull(m_FileHandler);
            ThreadPool.QueueUserWorkItem(_ => Internal_Load(mayBeEmpty, mayNotExist));
        }


        /// <summary>
        /// Loads the most recent persisted backup from the system disk for the
        /// configured root and profile.
        /// </summary>
        /// <param name="mayBeEmpty">
        /// <para>
        /// Indicates to the <c>Persister</c> that the file contents may not be
        /// present, the file is empty. It bypasses the typical exception check
        /// to prevent unwanted errors when it's known that the file may have no
        /// contents.
        /// </para>
        /// <para>
        /// An exception for the file not existing will still be thrown as it's
        /// expected that one exist if your going to read it. To bypass this as
        /// well, set the <paramref name="mayNotExist"/> parameter to true.
        /// </para>
        /// <para>
        /// When recording this problem, a <see cref="PersistFileExcpetion"/>
        /// will be provided in the <see cref="LoadEventArgs.Problem"/> property
        /// with no cause.
        /// </para>
        /// </param>
        /// <param name="mayNotExist">
        /// <para>
        /// Indicates to the <c>Persister</c> that the file may not be present
        /// in the file system at the time of reading. It bypasses the typical
        /// exception check to prevent unwanted errors when it's known that
        /// the file may not be present.
        /// </para>
        /// <para>
        /// When recording this problem, a <see cref="PersistFileException"/>
        /// will be provided in the <see cref="LoadEventArgs.Problem"/> property
        /// with a <see cref="FileNotFoundException"/>.
        /// </para>
        /// </param>
        /// <remarks>
        /// This will try all backups, and if no backup could be loaded, a
        /// generic <see cref="PersistException"/> will be provided in the
        /// <see cref="LoadEventArgs.Problem"/> property.
        /// </remarks>
        public void LoadBackup(bool mayBeEmpty = false, bool mayNotExist = false) {
            ThreadPool.QueueUserWorkItem(_ => Internal_LoadBackup(mayBeEmpty, mayNotExist));
        }


        /// <summary>
        /// Calls on demand save subscribers to finalize the data that will be
        /// written to the system disk, then creates or overwrites files as
        /// necessary.
        /// </summary>
        /// <remarks>
        /// If the file doesn't exist, it will create it; otherwise it will
        /// overwrite the contents in that file. If the data shouldn't be
        /// overwritten, then change the persistence file for this
        /// <c>Persister</c>.
        /// </remarks>
        public void Flush() {
            Assert.IsNotNull(m_FileHandler);
            ThreadPool.QueueUserWorkItem(_ => Internal_Flush());
        }


        /// <summary>
        /// Attempts to delete both the persistence data file and all of the
        /// backups that this <c>Persister</c> reads from and writes to.
        /// </summary>
        /// <remarks>
        /// Internally, this function calls the functions <see cref="Delete()"/>
        /// and <see cref="DeleteBackup()"/>, and as such, the
        /// <see cref="OnDelete"/> event will be triggered for each file that
        /// is deleted.
        /// </remarks>
        public void Purge() {
            Assert.IsNotNull(m_FileHandler);
            ThreadPool.QueueUserWorkItem(_ => Internal_Purge());
        }


        /// <summary>
        /// Attempts to delete the persistence file that this <c>Persister</c>
        /// reads from and writes to.
        /// </summary>
        public void Delete() {
            Assert.IsNotNull(m_FileHandler);
            ThreadPool.QueueUserWorkItem(_ => Internal_Delete());
        }


        /// <summary>
        /// Attempts to delete the backup persistence file, that this
        /// <c>Persister</c> creates for data integrity, with the provided
        /// index.
        /// </summary>
        /// <param name="index">
        /// The index of the backup persistence file that should be deleted.
        /// </param>
        /// <remarks>
        /// If the index is out of range, a <see cref="PersistException"/> will
        /// be provided in the <see cref="DeleteEventArgs.Problem"/> property.
        /// </remarks>
        public void DeleteBackup(int index) {
            Assert.IsNotNull(m_FileHandler);
            ThreadPool.QueueUserWorkItem(_ => Internal_DeleteBackup(index));
        }


    #region Persister Implementation
        internal bool HasKey(string key) {
            return m_Object?[key] != null;
        }

        internal void DeleteKey(string key) {
            m_Object.Remove(key);
        }

        internal void Serialise(string key, object value) {
            m_Object.Add(key, JToken.FromObject(value, M_SERIALIZER));
        }

        internal DataT Deserialize<DataT>(string key) {
            return m_Object[key].ToObject<DataT>(M_SERIALIZER);
        }


        private void Internal_SafeInvoke(
            PersistenceMethod method,
            EventHandler      handler,
            EventArgs         args
        ) {
            try {
                handler?.Invoke(this, args);
            } catch (Exception unhandled) {
                OnUnhandled?.Invoke(this, new() {
                    Cause = unhandled,
                    Method = method
                });
            }
        }


        private void Internal_SafeInvoke<EventArgsT>(
            PersistenceMethod        method,
            EventHandler<EventArgsT> handler,
            EventArgsT               args
        ) {
            try {
                handler?.Invoke(this, args);
            } catch (Exception unhandled) {
                OnUnhandled?.Invoke(this, new() {
                    Cause = unhandled,
                    Method = method
                });
            }
        }


        private void Internal_Load(bool mayBeEmpty, bool mayNotExist) {
            // Prep this.
            var loadArgs = new LoadEventArgs {
                Problem     = null,
                BackupCount = m_FileHandler.PersistenceBackups,
                BackupIndex = -1,
                NotFound    = false,
                EmptyFile   = false
            };

            try {
                var result = m_FileHandler.Read();
                result = EncryptionHandler.Decrypt(Encryption, m_Settings.encryptionPhrase, result);
                result = CompressionHandler.Decompress(Compression, result);
    
                // Handles the case where there is no data
                if (string.IsNullOrEmpty(result)) {
                    loadArgs.EmptyFile = true;
                    if (mayBeEmpty) {
                        m_Object = new();
                        Internal_SafeInvoke(PersistenceMethod.Load, OnLoad, loadArgs);
                        return;
                    }

                    loadArgs.Problem = new PersistFileException(
                        m_Settings.persistenceRoot,
                        m_Settings.persistenceProfile,
                        m_Settings.persistenceFile
                    );

                    Internal_SafeInvoke(PersistenceMethod.Load, OnLoad, loadArgs);
                    return;
                }

                m_Object = JObject.Parse(result);
            } catch (Exception cause) {
                if (
                    cause is DirectoryNotFoundException ||
                    cause is FileNotFoundException
                ) {
                    loadArgs.NotFound = true;
                    if (!mayNotExist) {
                        loadArgs.Problem = new PersistFileException(
                            m_Settings.persistenceRoot,
                            m_Settings.persistenceProfile,
                            m_Settings.persistenceFile,
                            cause
                        );
                    }
                } else {
                    loadArgs.Problem = new PersistException("Deserialization Failed", cause); 
                }
            }

            Internal_SafeInvoke(PersistenceMethod.Load, OnLoad, loadArgs);
        }


        private void Internal_LoadBackup(bool mayBeEmpty, bool mayNotExist) {
            // Prep this.
            var count    = m_FileHandler.PersistenceBackups;
            var loadArgs = new LoadEventArgs {
                Problem     = null,
                BackupCount = count,
                BackupIndex = -1
            };
     
            if (count == 0) {
                loadArgs.Problem = new PersistException("No backups available");
                loadArgs.BackupIndex = loadArgs.BackupCount;
                Internal_SafeInvoke(PersistenceMethod.LoadBackup, OnLoad, loadArgs);
                return;
            }

            var index = count;
            try {
                // Reverse iterate backups trying to load one of them.
                for (/* would have set index here */; index >= 0; --index) {
                    loadArgs.BackupIndex = index;
                    Internal_LoadBackup(mayBeEmpty, index);
                }

                // Did not read any backups.
                if (index == -1)
                    loadArgs.Problem = new PersistException("Could not load any backups");
            } catch (PersistFileException persist) {
                // For empty files when not allowed to skip.
                loadArgs.Problem = persist;
            } catch (Exception cause) {
                if (
                    cause is DirectoryNotFoundException ||
                    cause is FileNotFoundException
                ) {
                    loadArgs.NotFound = true;
                    if (!mayNotExist) {
                        loadArgs.Problem = new PersistFileException(
                            m_Settings.persistenceRoot,
                            m_Settings.persistenceProfile,
                            Path.ChangeExtension(m_Settings.persistenceFile, $".bkp.{index}"),
                            cause
                        );
                    }
                } else {
                    loadArgs.Problem = new PersistException("Deserialization Failed", cause);
                }
            }

            Internal_SafeInvoke(PersistenceMethod.LoadBackup, OnLoad, loadArgs);
        }


        private void Internal_LoadBackup(bool mayBeEmpty, int index) {
            var result = m_FileHandler.ReadBackup(index);
            result = EncryptionHandler.Decrypt(Encryption, m_Settings.encryptionPhrase, result);
            result = CompressionHandler.Decompress(Compression, result);

            // Handles the case where there is no data.
            if (string.IsNullOrEmpty(result)) {
                if (mayBeEmpty) {
                    m_Object = new();
                    return;
                }

                // Let caller handle this.
                throw new PersistFileException(
                    m_Settings.persistenceRoot,
                    m_Settings.persistenceProfile,
                    Path.ChangeExtension(m_Settings.persistenceFile, $".bkp.{index}")
                );
            }

            // If throws, caller will catch.
            m_Object = JObject.Parse(result);
        }


        private void Internal_Flush() {
            // Must call this for on demand save subscribers.
            Internal_SafeInvoke(PersistenceMethod.Flush, OnSave, EventArgs.Empty);
            var flushArgs = new FlushEventArgs {
                Problem     = null,
                BackupCount = m_FileHandler.PersistenceBackups,
                BackupIndex = -1,
            };

            string result;
            try {
                OnSave?.Invoke(this, EventArgs.Empty);
                if (!Directory.Exists(m_FileHandler.PersistencePath))
                    Directory.CreateDirectory(m_FileHandler.PersistencePath);

                result = JsonConvert.SerializeObject(m_Object, M_SERIALIZER_SETTINGS);
                result = CompressionHandler.Compress(Compression, result);
                result = EncryptionHandler.Encrypt(Encryption, m_Settings.encryptionPhrase, result);                
                m_FileHandler.Write(result);
            } catch (Exception cause) {
                flushArgs.Problem = new PersistException("Serialization Failed", cause);
                Internal_SafeInvoke(PersistenceMethod.Flush, OnFlush, flushArgs);
                return; // Do not proceed to write backups.
            }

            var index = 0;
            try {
                var persistFile = m_FileHandler.PersistenceFile;
                for (/* index */; index < m_Settings.maxBackups; index++) {
                    var backupFile = Path.ChangeExtension(persistFile, $".bkp.{index}");
                    File.Copy(persistFile, backupFile);
                }
            } catch (FileNotFoundException fileNotFound) {
                flushArgs.Problem = new PersistFileException(
                    m_Settings.persistenceRoot,
                    m_Settings.persistenceProfile,
                    m_Settings.persistenceFile,
                    fileNotFound
                );

                flushArgs.BackupIndex = index;
            } catch (Exception cause) {
                flushArgs.Problem     = new PersistException("Backup Failed", cause);
                flushArgs.BackupIndex = index;
            }

            Internal_SafeInvoke(PersistenceMethod.Flush, OnFlush, flushArgs);
        }


        private void Internal_Delete() {
            var deleteArgs = new DeleteEventArgs {
                Problem = null,
                BackupCount = m_FileHandler.PersistenceBackups,
                BackupIndex = -1
            };

            try {
                m_FileHandler.Delete();
            } catch (Exception cause) {
                deleteArgs.Problem = new PersistException("Deletion Failed", cause);
            }

            Internal_SafeInvoke(PersistenceMethod.Delete, OnDelete, deleteArgs);
        }


        private void Internal_DeleteBackup(int index) {
            var count = m_FileHandler.PersistenceBackups;
            var deleteArgs = new DeleteEventArgs {
                Problem     = null,
                BackupCount = count,
                BackupIndex = index
            };

            if (index < 0 || count <= index) {
                deleteArgs.Problem = new PersistException(
                    "Invalid backup index",
                    new IndexOutOfRangeException()
                );
                Internal_SafeInvoke(PersistenceMethod.DeleteBackup, OnDelete, deleteArgs);
                return;
            }

            try {
                m_FileHandler.DeleteBackup(index);
            } catch (Exception cause) {
                deleteArgs.Problem = new PersistException("Backup Deletion Failed", cause);
            }

            Internal_SafeInvoke(PersistenceMethod.DeleteBackup, OnDelete, deleteArgs);
        }


        private void Internal_Purge() {
            Internal_Delete();
            for (var index = 0; index < m_FileHandler.PersistenceBackups; index++)
                Internal_DeleteBackup(index);
        }
    #endregion
    }
}