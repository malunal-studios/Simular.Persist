# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-06-05

### Added

- `PersistenceManager` for Unity runtime support.
- Persistence reader for `Persister` cached JSON object.
- Persistence writer for `Persister` cached JSON object.
- Primary persistence functions: Load, Save, Flush, and Delete.
- Support AES encryption using passphrase.
- Support GZip compression.
