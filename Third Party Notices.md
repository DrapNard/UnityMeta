# Third Party Notices

UnityMeta depends on the following external components but does not vendor their
source code in this repository.

## Mono.Cecil

The Unity package depends on `com.unity.nuget.mono-cecil` for IL inspection and
rewriting inside the Unity Editor. Mono.Cecil is distributed under the MIT
license.

## Microsoft.CodeAnalysis

The optional compiler companion targets Microsoft.CodeAnalysis 3.8 because that
is the Roslyn analyzer/source-generator version required by Unity 2022's
supported workflow.

## PolySharp

The optional compiler companion references PolySharp as a private build-time
package. PolySharp is MIT-licensed and provides source-only polyfills for newer
C# compiler support types when targeting older frameworks. It does not replace
Unity's C# compiler.
