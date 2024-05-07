using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Simular.Persist;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class GenericPersistenceTests {
    [OneTimeTearDown]
    public static void AfterAll() {
        // Final clean up step, leave clean environment for next round of
        // testing for any of the tests.
        var testingDir = Path.Join(Application.persistentDataPath, "testing");
        if (Directory.Exists(testingDir))
            Directory.Delete(testingDir, true);
    }


    [UnityTest]
    public IEnumerator Persister_WhenConfigured_CanLoadWithoutDirectory() {
        var threadRunning = true;
        var testingDir = Path.Join(Application.persistentDataPath, "testing");
        if (Directory.Exists(testingDir))
            Directory.Delete(testingDir);

        var persister = new Persister(new() {
            persistenceRoot = "testing"
        });

        var argsCache = new Persister.LoadEventArgs();
        var directoryFound = false;
        void onLoadHandler(object p, Persister.LoadEventArgs l) {
            argsCache = l;
            directoryFound = Directory.Exists(testingDir);
            Persister.OnLoad -= onLoadHandler;
            threadRunning = false;
        }
        
        Persister.OnLoad += onLoadHandler;
        persister.Load(mayNotExist: true); // mayNotExist is important here!
        while (threadRunning)
            yield return null;
        
        Assert.IsNull(argsCache.Problem, "Failed to load data");
        Assert.IsTrue(argsCache.BackupCount == 0, "Unexpected backup value.");
        Assert.IsTrue(argsCache.BackupIndex == -1, "Unexpected backup value.");
        Assert.IsFalse(directoryFound, "Directory existed during test.");
    }


    [UnityTest]
    public IEnumerator Persister_WhenConfigured_PerformsCriticalTasks() {
        var threadRunning = false;
        var persister = new Persister(new() {
            persistenceRoot = "testing"
        });


    #region Save Test
        var flushArgsCache = new Persister.FlushEventArgs();
        void onSaveHandler(object p, EventArgs _) {
            var writer = new PersistenceWriter((Persister)p);
            writer.Write("test", "my-test-value");
            Persister.OnSave -= onSaveHandler;
        }

        void onFlushHandler(object p, Persister.FlushEventArgs f) {
            flushArgsCache = f;
            Persister.OnFlush -= onFlushHandler;
            threadRunning = false;
        };
        
        threadRunning = true;
        Persister.OnSave += onSaveHandler;
        Persister.OnFlush += onFlushHandler;
        persister.Flush();
        while (threadRunning)
            yield return null;

        Assert.IsNull(flushArgsCache.Problem, "Failed to flush data.");
        Assert.IsTrue(flushArgsCache.BackupCount == 0, "Wrote backups when not requested.");
        Assert.IsTrue(flushArgsCache.BackupIndex == -1, "Wrote backups when not requested.");
    #endregion

    #region Load Test
        var loadArgsCache = new Persister.LoadEventArgs();
        void onLoadHandler(object p, Persister.LoadEventArgs l) {
            loadArgsCache = l;
            Persister.OnLoad -= onLoadHandler;
            threadRunning = false;
        }

        threadRunning = true;
        Persister.OnLoad += onLoadHandler;
        persister.Load();
        while (threadRunning)
            yield return null;

        Assert.IsNull(loadArgsCache.Problem, "Failed to load data.");
        Assert.IsTrue(loadArgsCache.BackupCount == 0, "Unexpected backup value.");
        Assert.IsTrue(loadArgsCache.BackupIndex == -1, "Unexpected backup value.");

        var reader = new PersistenceReader(persister);
        var value  = reader.Read<string>("test");
        Assert.IsFalse(string.IsNullOrEmpty(value), "Did not load data properly.");
        Assert.IsTrue(value == "my-test-value", "Did not load data properly.");
    #endregion

    #region Delete Test
        var deleteArgsCache = new Persister.DeleteEventArgs();
        void onDeleteHandler(object p, Persister.DeleteEventArgs d) {
            deleteArgsCache = d;
            Persister.OnDelete -= onDeleteHandler;
            threadRunning = false;
        }

        threadRunning = true;
        Persister.OnDelete += onDeleteHandler;
        persister.Delete();
        while (threadRunning)
            yield return null;
        
        Assert.IsNull(deleteArgsCache.Problem, "Failed to delete data.");
        Assert.IsTrue(deleteArgsCache.BackupCount == 0, "Unexpected backup value.");
        Assert.IsTrue(deleteArgsCache.BackupIndex == -1, "Unexpected backup value.");
    #endregion
    }


    [UnityTest]
    public IEnumerator Persister_WhenEventThrows_CallsOnUnhandled() {
        var threadRunning = false;
        var persister = new Persister(new() {
            persistenceRoot = "testing"
        });

        var unhandledArgsCache = new Persister.UnhandledEventArgs();
        void onUnhandledHandler(object p, Persister.UnhandledEventArgs args) {
            unhandledArgsCache = args;
            threadRunning = false;
        }

        Persister.OnUnhandled += onUnhandledHandler;

    #region Unhandled Load Exception
        void onLoadHandler(object p, Persister.LoadEventArgs args) {
            Persister.OnLoad -= onLoadHandler;
            throw new Exception("My unhandled exception");
        }

        threadRunning = true;
        Persister.OnLoad += onLoadHandler;
        persister.Load();
        while (threadRunning)
            yield return null;
        
        Assert.IsNotNull(
            unhandledArgsCache.Cause,
            "Unhandled exception was not caught."
        );

        Assert.AreEqual(
            unhandledArgsCache.Cause.Message,
            "My unhandled exception",
            "Unhandled exception was not the expected one."
        );
        
        Assert.AreEqual(
            unhandledArgsCache.Method,
            PersistenceMethod.Load,
            "Unhandled exception did not come from Load method."
        );
    #endregion

    #region Unhandled Load Backup Exception
        threadRunning = true;
        Persister.OnLoad += onLoadHandler;
        persister.LoadBackup();
        while (threadRunning)
            yield return null;
        
        Assert.IsNotNull(
            unhandledArgsCache.Cause,
            "Unhandled exception was not caught."
        );

        Assert.AreEqual(
            unhandledArgsCache.Cause.Message,
            "My unhandled exception",
            "Unhandled exception was not the expected one."
        );
        
        Assert.AreEqual(
            unhandledArgsCache.Method,
            PersistenceMethod.LoadBackup,
            "Unhandled exception did not come from Load method."
        );
    #endregion

    #region Unhandled Save Exception
        void onSaveHandler(object p, EventArgs args) {
            Persister.OnSave -= onSaveHandler;
            throw new Exception("My unhandled exception");
        }

        threadRunning = true;
        Persister.OnSave += onSaveHandler;
        persister.Flush();
        while (threadRunning)
            yield return null;

        Assert.IsNotNull(
            unhandledArgsCache.Cause,
            "Unhandled exception was not caught."
        );

        Assert.AreEqual(
            unhandledArgsCache.Cause.Message,
            "My unhandled exception",
            "Unhandled exception was not the expected one."
        );
        
        Assert.AreEqual(
            unhandledArgsCache.Method,
            PersistenceMethod.Flush,
            "Unhandled exception did not come from Load method."
        );
    #endregion

    #region Unhandled Flush Exception
        void onFlushHandler(object p, Persister.FlushEventArgs args) {
            Persister.OnFlush -= onFlushHandler;
            throw new Exception("My unhandled exception");
        }

        threadRunning = true;
        Persister.OnFlush += onFlushHandler;
        persister.Flush();
        while (threadRunning)
            yield return null;

        Assert.IsNotNull(
            unhandledArgsCache.Cause,
            "Unhandled exception was not caught."
        );

        Assert.AreEqual(
            unhandledArgsCache.Cause.Message,
            "My unhandled exception",
            "Unhandled exception was not the expected one."
        );
        
        Assert.AreEqual(
            unhandledArgsCache.Method,
            PersistenceMethod.Flush,
            "Unhandled exception did not come from Load method."
        );
    #endregion

    #region Unhandled Delete Exception
        void onDeleteHandler(object p, EventArgs args) {
            Persister.OnDelete -= onDeleteHandler;
            throw new Exception("My unhandled exception");
        }

        threadRunning = true;
        Persister.OnDelete += onDeleteHandler;
        persister.Delete();
        while (threadRunning)
            yield return null;

        Assert.IsNotNull(
            unhandledArgsCache.Cause,
            "Unhandled exception was not caught."
        );

        Assert.AreEqual(
            unhandledArgsCache.Cause.Message,
            "My unhandled exception",
            "Unhandled exception was not the expected one."
        );
        
        Assert.AreEqual(
            unhandledArgsCache.Method,
            PersistenceMethod.Delete,
            "Unhandled exception did not come from Load method."
        );
    #endregion

    #region Unhandled Delete Backup Exception
        threadRunning = true;
        Persister.OnDelete += onDeleteHandler;
        persister.DeleteBackup(0);
        while (threadRunning)
            yield return null;
        
        Assert.IsNotNull(
            unhandledArgsCache.Cause,
            "Unhandled exception was not caught."
        );

        Assert.AreEqual(
            unhandledArgsCache.Cause.Message,
            "My unhandled exception",
            "Unhandled exception was not the expected one."
        );
        
        Assert.AreEqual(
            unhandledArgsCache.Method,
            PersistenceMethod.DeleteBackup,
            "Unhandled exception did not come from Load method."
        );
    #endregion

        // Cleanup.
        Persister.OnUnhandled -= onUnhandledHandler;
    }
}
