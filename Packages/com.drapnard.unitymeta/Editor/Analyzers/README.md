# Optional compiler companion

`UnityMeta.Compiler.dll` is intentionally not committed as a generated binary.
Build it with `./build.sh`, then run `./tools/install-compiler.sh`.

The core IL-weaving package works without this DLL. The compiler companion adds
Roslyn diagnostics and a generated aspect manifest, and is the future backend
for source-visible member introduction.
