# Simular.Persist

This is a package for modern Unity 2022.3+ for saving games and file system based data persistence. This package is designed to work well with specifically saving to the file system in a pragmatic way. It's designed to be efficient and not hinder the operations of Unity by executing all file operations on a separate thread, notifying listeners when complete. It provides a singleton class `PersistenceManager`, which can be extended, that provides a basis for doing persistence management during runtime. The `PersistenceManager` also supports auto-saving, you need only enable it and set the interval. There are other settings that you'll find for the `PersistenceManager` that will configure the `Persister` which handles the final persistence to the file system.

## Installation

Installing this package is simple. With a compatible version of Unity open, open the Unity Package Manager (`Window > Package Manager`), click the `+` button, and select `Add package from GIT url`. Enter the URL of this GitHub repo and you're good to go.

## Usage

### Configuration

Below is how you can configure your own `Persister` if you need more than one, or if you need your own custom code. You'll need a `PersistenceReader` and a `PersistenceWriter` to read and write to the `Persister`. The `Persister` can facilitate: **loading**, **saving**, **flushing**, and **deletion** of persistence data to/from the file system. All data from a loaded persisted file (data or backup) is cached in memory making it more efficient to read and write. All changes that you make to the `Persister` must be flushed in order for it to be written to the file system and backups made.

```cs
using UnityEngine;
using Simular.Persist;

public class MyMonoBehaviour : MonoBehaviour {
    private Persister         m_MyCustomPersister;
    private PersistenceReader m_MyCustomReader;
    private PersistenceWriter m_MyCustomWriter;

    private void Awake() {
        // Inner struct is of type Persister.Settings.
        m_MyCustomPersister = new Persister(new() {
            persistenceRoot    = "custom-saves",
            persistenceProfile = "player-username",
            persistenceFile    = "save-game-xxxx",
            encryptionPhrase   = "MeyeC00!P@$spH4as3",
            maxBackups         = 1,
            compression        = CompressionMethod.GZip,
            encryption         = EncryptionMethod.Aes
        });

        m_MyCustomReader = new PersistenceReader(m_MyCustomPersister);
        m_MyCustomWriter = new PersistenceWriter(m_MyCustomPersister);

        m_MyCustomWriter.Write("my_int", 15);
        m_MyCustomPersister.Flush();
    }
}
```

### Loading

Below is an example of loading data from disk, given that the approriate settings have been configured on the `PersistenceManager`.

```cs
using UnityEngine;
using Simular.Persist;

public class MyMonoBehaviour : MonoBehaviour {
    private void Start() {
        // Be careful when calling Unity specific code, as this event will be
        // executed from a separate thread and not all Unity functions are
        // thread safe. Debug.Log is luckily.
        Persister.OnLoad += HandleLoad;
        PersistenceManager.Persister.Load(mayNotExist: true);
    }

    private void HandleLoad(object persister, Persister.LoadEventArgs loadArgs) {
        if (loadArgs.Problem == null)
            Debug.Log("Successfully loaded save data");
        else Debug.Log("Failed to load save data");
    }
}
```

### Saving & Flushing

Below is an example of saving data to disk, given that the appropriate settings have been configured on the `PersistenceManager`.

```cs
using System;
using UnityEngine;
using Simular.Persist;

public class MyMonoBehviour : MonoBehaviour {
    private bool m_CustomData;

    private void Start() {
        Persister.OnSave += HandleSave;
        Persister.OnFlush += HandleFlush;
        PersistenceManager.Persister.Flush();
    }

    private void HandleSave(object persister, EventArgs _) {
        var writer = new PersistenceWriter((Persister)persister);
        writer.Write("custom_data", m_CustomData);
    }

    private void HandleFlush(object persister, Persister.FlushEventArgs flushArgs) {
        if (flushArgs.Problem == null)
            Debug.Log("Successfully flushed save data");
        else Debug.Log("Failed to flush save data");
    }
}
```

### Deletion

Below is an example of deleting data from disk, given that the appropriate settings have been configured on the `PersistenceManager`.

```cs
using UnityEngine;
using Simular.Persist;

public class MyMonoBehaviour : MonoBehaviour {
    private void OnDestroy() {
        // Purge deletes all persisted files of a given name.
        // The name is provided in Persister.Settings.persistenceFile.
        Persister.OnDelete += HandleDelete;
        PersistenceManager.Persister.Purge();
        
        // If you need to delete things manually, you can use the below
        // functions:
        // PersistenceManager.Persister.Delete();
        // PersistenceManager.Persister.DeleteBackup(index: 1);
    }

    private void HandleDelete(object persister, Persister.DeleteEventArgs deleteArgs) {
        if (deleteArgs.Problem == null)
            Debug.Log("Successfully deleted save data");
        else Debug.Log("Failed to delete save data.");
    }
}
```
