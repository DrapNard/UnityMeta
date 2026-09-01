# Publishing

UnityMeta has two distribution surfaces with different responsibilities.

## UPM - primary Unity distribution

The full UPM package contains runtime authoring APIs, the Unity IL post-processor/weaver,
and on releases the precompiled Roslyn companion.

A `vX.Y.Z` source tag triggers `.github/workflows/release.yml`. The workflow:

1. builds and runs all standalone smoke tests;
2. copies `UnityMeta.Compiler.dll` into the package with the `RoslynAnalyzer` label;
3. creates `com.drapnard.unitymeta-X.Y.Z.tgz` and SHA-256 checksums;
4. creates a GitHub Release;
5. publishes a package-root `upm` branch;
6. creates an `upm-vX.Y.Z` tag pointing to that precompiled package root;
7. optionally pushes `UnityMeta.Authoring` to nuget.org.

Install an exact UPM build with:

```text
https://github.com/DrapNard/UnityMeta.git#upm-vX.Y.Z
```

## NuGet - authoring/tooling only

`UnityMeta.Authoring` contains:

- `UnityMeta.Runtime.dll` under `lib/netstandard2.0`;
- `UnityMeta.Compiler.dll` under `analyzers/dotnet/cs`.

It intentionally does **not** pretend to weave ordinary .NET projects and does not replace
Unity's UPM package. Publishing to nuget.org occurs only when the repository secret
`NUGET_API_KEY` exists.

## Creating a release

After CI is green and `package.json` contains the intended version:

```bash
git tag v0.2.0-alpha.4
git push origin v0.2.0-alpha.4
```

The tag version must exactly match `Packages/com.drapnard.unitymeta/package.json`.
