# Changelog

## 0.2.0-alpha.5

### Fixed

- Cecil short-branch/macro normalization around weaving prevents invalid IL when aspects
  substantially enlarge methods or generated state-machine `MoveNext` bodies.

## 0.2.0-alpha.4

### Fixed

- Removed `.meta` files from `Samples~` and `Documentation~`; Unity intentionally ignores tilde-suffixed package trees and does not track them with metadata.
- Set the `Unity.DrapNard.UnityMeta.CodeGen` assembly to `autoReferenced: false`, as required by Unity for CodeGen assemblies.
- Hardened package validation against both regressions.

## 0.2.0-alpha.3

- Added stable Unity `.meta` files across the immutable Git/UPM package so Unity 2022.3
  imports Runtime, Editor and asmdef assets instead of ignoring them.
- Fixed the Roslyn companion importer so it is compiler-only and is not loaded as a normal
  managed plugin.
- Added release validation preventing missing metadata from being published again.

## 0.2.0-alpha.2

- Fixed field-change equality weaving on modern .NET/CLR runtimes by routing comparisons through `MetaRuntimeServices.AreEqual<T>`.
- Fixed Roslyn analyzer release-tracking metadata format.

## 0.2.0-alpha.1

- Added generic field-get/read transformations for ordinary field loads.
- Added generic field-change/OnChange primitives with old/new values and real-change
  filtering.
- Added return-value observation, named aspect metadata, and richer constant metadata.
- Expanded compiler diagnostics and samples.
- Added precompiled release packaging support.

## 0.1.0

Initial metaprogramming MVP. See the repository root changelog for details.
