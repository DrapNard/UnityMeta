# PolySharp and Unity 2022

PolySharp is useful here, but it solves a narrower problem than replacing Unity's
compiler.

## What PolySharp does

PolySharp is a source generator that supplies compiler/BCL support types such as
`IsExternalInit`, nullability attributes, required-member attributes and other
source-only polyfills when targeting older frameworks.

UnityMeta uses PolySharp privately in the optional external compiler companion,
which targets .NET Standard 2.0.

## What PolySharp does not do

PolySharp does **not** replace the C# parser bundled with Unity 2022. If Unity's
Roslyn version does not understand a newer syntax feature, generating the support
type for that feature is not enough to teach the parser the syntax.

Therefore UnityMeta package/runtime source remains C# 9 compatible.

## Policy

- Package source consumed directly by Unity: C# 9.
- External build tools: may use a newer SDK, with PolySharp where useful for
  downlevel target-framework types.
- Never make gameplay correctness depend on a polyfill that Unity cannot compile.
