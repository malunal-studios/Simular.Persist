using NUnit.Framework;
using Simular.Persist;

[TestFixture]
public class GenericPersistenceTests {
    [Test]
    public void Persister_WhenConfigured_PerformsCriticalTasks() {
        var persister = new Persister(new() {
            persistenceRoot = "testing"
        });

    #region Save Test
        Persister.OnSave += (p, _) => {
            var writer = new PersistenceWriter((Persister)p);
            writer.Write("test", "my-test-value");
        };

        Persister.OnFlush += (p, a) => {
            Assert.IsNull(a.Problem, "Failed to flush data.");
            Assert.IsTrue(a.BackupCount == 0, "Wrote backups when not requested.");
            Assert.IsTrue(a.BackupIndex == -1, "Wrote backups when not requested.");
        };

        persister.Flush();
    #endregion

    #region Load Test
        Persister.OnLoad += (p, l) => {
            Assert.IsNull(l.Problem, "Failed to load data.");
            Assert.IsTrue(l.BackupCount == 0, "Unexpected backup value.");
            Assert.IsTrue(l.BackupIndex == -1, "Unexpected backup value.");

            var reader = new PersistenceReader((Persister)p);
            var value  = reader.Read<string>("test");
            Assert.IsTrue(string.IsNullOrEmpty(value), "Did not load data properly.");
            Assert.IsTrue(value == "my-test-value", "Did not load data properly.");
        };

        persister.Load();
    #endregion

    #region Delete Test
        Persister.OnDelete += (p, d) => {
            Assert.IsNull(d.Problem, "Failed to delete data.");
            Assert.IsTrue(d.BackupCount == 0, "Unexpected backup value.");
            Assert.IsTrue(d.BackupIndex == -1, "Unexpected backup value.");
        };

        persister.Delete();
    #endregion
    }
}
