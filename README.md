GrfUnpack
=============================================================================

A simple unpacker for the PAK archive files used by the game Arcturus and
the old GRF files used by early clients of the MMORPG Ragnarok Online.

Usage
-----------------------------------------------------------------------------

```text
GrfUnpack.exe [options] <file> [output folder]
```

### file
The archive to unpack.

### output folder (optional)
The path to extract files to. Defaults to './output', relative to the
working directory.

### options (optional)
#### -k
If set, the extracted files will retain their original Korean encoding.

Examples
-----------------------------------------------------------------------------

Extract the contents of data.pak to the arcturus sub-folder.
```text
GrfUnpack.exe data.pak arcturus
```

Extract the contents of pdata000.grf to the output sub-folder,
using Korean file names.
```text
GrfUnpack.exe -k pdata000.grf
```
