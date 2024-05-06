using System.IO;
using System.Linq;
using UnityEngine;

namespace Simular.Persist {
    /// <summary>
    /// Responsible for handling file specific operations such as reading and
    /// writing to the main persistence data file, or one of its backups.
    /// </summary>
    internal sealed class FileHandler {
        /// <summary>
        /// The full parent path of the persistence file, where the main file
        /// and all of the backups for the persistence data will be read from
        /// and written to.
        /// </summary>
        public string PersistencePath { get; private set; }

        /// <summary>
        /// The full path to the primary persistence data file that data will
        /// be read from and written to by this handler.
        /// </summary>
        public string PersistenceFile { get; private set; }

        /// <summary>
        /// The maximum number of backups that this handler should create for
        /// any given persistence file.
        /// </summary>
        public int MaxPersistenceBackups { get; private set; }

        /// <summary>
        /// The number of known backups for the primary persistence data file.
        /// </summary>
        public int PersistenceBackups =>
            Directory.EnumerateFiles(PersistencePath, "*.bkp.*")
                     .Select(IsSameFile)
                     .Count();

        /// <summary>
        /// Checks if there are any persistence backups available in the
        /// persistence path that this <c>FileHandler</c> manages.
        /// </summary>
        public bool HasPersistenceBackups => PersistenceBackups != 0;


        /// <summary>
        /// Creates a new <c>FileHandler</c> from the given persistence root,
        /// persistence profile, and persistence file name.
        /// </summary>
        /// <param name="root">
        /// The root directory that contains the persistence profiles and the
        /// persistence files.
        /// </param>
        /// <param name="profile">
        /// The name of the profile the persistence data will be read from and
        /// written to by this handler.
        /// </param>
        /// <param name="file">
        /// The name of the file the persistence data will be read from and
        /// written to by this handler.
        /// </param>
        public FileHandler(
            string root,
            string profile,
            string file,
            int    maxBackups
        ) {
            PersistencePath = Path.Join(Application.persistentDataPath, root, profile);
            PersistenceFile = Path.Join(PersistencePath, file);
            PersistenceFile = Path.ChangeExtension(PersistenceFile, ".dat");
            MaxPersistenceBackups = maxBackups;
        }


        /// <summary>
        /// Reads the entire contents of the persistence file
        /// </summary>
        /// <returns>
        /// The full contents of the persistence file.
        /// </returns>
        public string Read() {
            // Will throw FileNotFoundException if the persistence file doesn't
            // exist. This should be handled by the Persister.
            using var stream = new FileStream(PersistenceFile, FileMode.Open);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }


        /// <summary>
        /// Reads the entire contents of the backup file provided.
        /// </summary>
        /// <param name="index">
        /// The index of the backup file to read.
        /// </param>
        /// <returns>
        /// The full contents of the backup file.
        /// </returns>
        public string ReadBackup(int index) {
            var backupFile = Path.ChangeExtension(PersistenceFile, $".bkp.{index}");
            using var stream = new FileStream(backupFile, FileMode.Open);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }


        /// <summary>
        /// Writes the data to the persistence file.
        /// </summary>
        /// <param name="data">
        /// The data that will overwrite the persistence file.
        /// </param>
        /// <remarks>
        /// Either creates a new file if one does not exist, or truncates the
        /// existing file to overwrite the contents.
        /// </remarks>
        public void Write(string data) {
            using var stream = new FileStream(PersistenceFile, FileMode.Create);
            using var writer = new StreamWriter(stream);
            writer.Write(data);
        }


        /// <summary>
        /// Deletes the file that this handler is managing.
        /// </summary>
        public void Delete() {
            File.Delete(PersistenceFile);
        }


        /// <summary>
        /// Deletes one of the backup persistence files with the given index.
        /// </summary>
        /// <param name="index">
        /// The index of the backup persistence file to delete.
        /// </param>
        public void DeleteBackup(int index) {
            File.Delete(Path.ChangeExtension(PersistenceFile, $".bkp.{index}"));
        }


        private bool IsSameFile(string filePath) {
            return Path.ChangeExtension(filePath, ".dat") == PersistenceFile;
        }
    }
}