# Roslyn companion

Release builds place `UnityMeta.Compiler.dll` in this folder. Its adjacent `.meta` file is
tracked so Unity imports the DLL with the `RoslynAnalyzer` label.

Source checkouts intentionally do not commit the binary. Build/install it with:

```bash
./build.sh
./tools/install-compiler.sh
```
