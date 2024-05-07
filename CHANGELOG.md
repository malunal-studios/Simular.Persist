# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2024-06-05

Some changes in this version are considered breaking and would normally consitute a new minor or major version, but since this is so new and it won't affect developers yet, we're gonna consider it a patch as a few of them were just wrong anyways, and their updated versions are the correct ones.

### Fixed

- `FileHandler` throws `DirectoryNotFoundException` when no persistence directory exists to count backups for persistence file.
- Most of the `Persister` thread internal functions would also throw `DirectoryNotFoundException` which wasn't being caught and converted to `PersistFileException`.
- Tests didn't work properly because NUnit throws exceptions from it's assertions, that when the test function is bootstrapped, would not get caught from the separate thread. Tests have been reworked to cache results of the worker thread and wait for it to complete before performing assertions.
- `Persister.IListener.OnFlush` accepted incorrect arguments to be used for `Persister.OnFlush` event.
- Various typos and missing documentation.

### Added

- Test the `Persister.Load` without persistence path existing to make sure it can still report non-existent file.
- `Persister.IListener.OnDelete` interface function for compatibility with `Persister.OnDelete` event.

### Removed

- `Persister.m_Dirty` and `Persister.IsLoaded` would cause issues for testing and typical use and were found to be unnecessary.

## [1.0.0] - 2024-06-05

### Added

- `PersistenceManager` for Unity runtime support.
- Persistence reader for `Persister` cached JSON object.
- Persistence writer for `Persister` cached JSON object.
- Primary persistence functions: Load, Save, Flush, and Delete.
- Support AES encryption using passphrase.
- Support GZip compression.
