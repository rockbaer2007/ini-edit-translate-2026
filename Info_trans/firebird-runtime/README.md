# Restored Firebird runtime

The x86 directory preserves the Firebird 2.5.8 native binaries and support files
from the archived 2019 Info_trans release. They are copied into help at build time.
The old executable, database logs and local aliases are excluded.

Info_trans is an x86 process. The main editor can still launch it independently
of its own process architecture. The managed Firebird provider is restored by NuGet
as 7.5.0 and its license is under help/licenses.

The reference database is retained separately under reference-data/IET2.FDB.
Tests query only working copies. Packaging restores the untouched source database
and excludes runtime-generated log files.
