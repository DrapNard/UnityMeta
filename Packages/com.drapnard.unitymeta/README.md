# UnityMeta UPM package

UnityMeta provides user-authored metaprogramming primitives for Unity 2022.3.

It includes:

- field-get transformation aspects;
- field-set transformation aspects;
- field-change/OnChange aspects with old/new values;
- method before/after aspects and return-value observation;
- Unity IL post-processing through Mono.Cecil;
- optional Roslyn 3.8 diagnostics/source generation in precompiled releases.

The package intentionally does not hard-code gameplay features. Import `Clamp`, `Read Transform`, `OnChange`
or `Log` from **Samples** to see complete aspects authored with the public API.

For normal installation, prefer a precompiled `upm-v*` Git tag from the UnityMeta
repository. See `Documentation~/index.md` and the repository documentation for details.
