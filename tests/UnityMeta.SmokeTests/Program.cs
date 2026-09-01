using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using UnityMeta.Compiler;
using UnityMeta.Weaver;

internal static class Program
{
    private const string Fixture = @"
using System;
using System.Threading.Tasks;
using UnityMeta;

public sealed class ClampAttribute : FieldSetAspectAttribute
{
    public ClampAttribute(int min, int max) { }
    public ClampAttribute(int min, string maxField) { }

    [SetTemplate]
    public static int Constant(
        [Value] int value,
        [AspectArgument(0)] int min,
        [AspectArgument(1)] int max)
        => value < min ? min : value > max ? max : value;

    [SetTemplate]
    public static int Dynamic(
        [Value] int value,
        [AspectArgument(0)] int min,
        [FieldValueFromAspectArgument(1)] int max)
        => value < min ? min : value > max ? max : value;
}

public sealed class OffsetReadAttribute : FieldGetAspectAttribute
{
    public OffsetReadAttribute(int offset) { }

    [GetTemplate]
    public static int Apply(
        [Value] int value,
        [AspectArgument(0)] int offset)
        => value + offset;
}

public sealed class TrackChangeAttribute : FieldChangeAspectAttribute
{
    public string Tag { get; set; } = string.Empty;

    [ChangeTemplate]
    public static void Changed(
        [TargetInstance] Combat target,
        [OldValue] int oldValue,
        [NewValue] int newValue,
        [AspectNamedArgument(""Tag"")] string tag)
    {
        target.ChangeCount++;
        target.LastOldValue = oldValue;
        target.LastNewValue = newValue;
        target.LastTag = tag;
    }
}

public sealed class CountCallsAttribute : MethodAspectAttribute
{
    public CountCallsAttribute(Type marker) { }
    public string Label { get; set; } = string.Empty;

    [BeforeTemplate]
    public static void Before(
        [TargetInstance] Combat target,
        [AspectArgument(0)] Type marker,
        [AspectNamedArgument(""Label"")] string label)
    {
        target.BeforeCount++;
        target.LastMetadata = marker.Name + "":"" + label;
    }

    [AfterTemplate]
    public static void After(
        [TargetInstance] Combat target,
        [ReturnValue] int result)
    {
        target.AfterCount++;
        target.LastReturnValue = result;
    }
}

public sealed class Combat
{
    [Clamp(0, 100)]
    [TrackChange(Tag = ""fixed-hp"")]
    public int FixedHp;

    [Clamp(0, nameof(MaxHp))]
    public int Hp;

    [OffsetRead(5)]
    public int Displayed;

    public int MaxHp = 100;
    public int ChangeCount;
    public int LastOldValue;
    public int LastNewValue;
    public string LastTag = string.Empty;
    public int BeforeCount;
    public int AfterCount;
    public int LastReturnValue;
    public string LastMetadata = string.Empty;

    public void SetFixedHp(int value) { FixedHp = value; }
    public void SetHp(int value) { Hp = value; }
    public void SetDisplayed(int value) { Displayed = value; }
    public int ReadDisplayed() { return Displayed; }

    public void StressShortBranch(bool skip)
    {
        if (skip)
        {
            return;
        }

        // Keep the source body compact enough for Roslyn to emit a short branch.
        // Each assignment expands substantially once Clamp + TrackChange are woven.
        FixedHp = 1000;
        FixedHp = 1001;
        FixedHp = 1002;
        FixedHp = 1003;
        FixedHp = 1004;
        FixedHp = 1005;
        FixedHp = 1006;
        FixedHp = 1007;
    }

    public async Task<int> StressStateMachine(bool skip)
    {
        await Task.Yield();
        if (skip)
        {
            return FixedHp;
        }

        FixedHp = 1100;
        FixedHp = 1101;
        FixedHp = 1102;
        FixedHp = 1103;
        FixedHp = 1104;
        FixedHp = 1105;
        return FixedHp;
    }

    [CountCalls(typeof(Combat), Label = ""attack"")]
    public int Attack(int damage)
    {
        return damage * 2;
    }
}
";

    private const string InvalidFixture = @"
using UnityMeta;

public sealed class BrokenSetAttribute : FieldSetAspectAttribute
{
    [SetTemplate]
    public int NotStatic([Value] int value) => value;

    [SetTemplate]
    public static int MissingBinding(int value) => value;

    [SetTemplate]
    public static int Generic<T>([Value] int value) => value;

    [SetTemplate]
    private static int Private([Value] int value) => value;

    [ChangeTemplate]
    public static void WrongBase([OldValue] int value) { }
}

public sealed class BrokenGetAttribute : FieldGetAspectAttribute
{
    [GetTemplate]
    public static void NoReturn([Value] int value) { }

    [GetTemplate]
    public static int InvalidBinding([OldValue] int value) => value;
}

public sealed class BrokenChangeAttribute : FieldChangeAspectAttribute
{
    [ChangeTemplate]
    public static int WrongReturn([OldValue] int value) => value;
}

public sealed class BrokenMethodAttribute : MethodAspectAttribute
{
    [BeforeTemplate]
    [AfterTemplate]
    public static void TwoRoles() { }
}
";

    public static int Main()
    {
        try
        {
            RunCompilerCompanionAssertions();

            byte[] original = Emit(CreateCompilation(Fixture, "UnityMeta.SmokeFixture"));
            byte[] woven = Weave(original);
            ExecuteRuntimeAssertions(woven);

            Console.WriteLine("UnityMeta smoke tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void RunCompilerCompanionAssertions()
    {
        CSharpCompilation validCompilation = CreateCompilation(Fixture, "UnityMeta.CompilerFixture");

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AspectManifestGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            validCompilation,
            out Compilation generatedCompilation,
            out ImmutableArray<Diagnostic> generatorDiagnostics);

        if (generatorDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException(
                "Source generator diagnostics:" + Environment.NewLine +
                string.Join(Environment.NewLine, generatorDiagnostics.Select(diagnostic => diagnostic.ToString())));
        }

        string generated = string.Join(
            Environment.NewLine,
            generatedCompilation.SyntaxTrees.Skip(validCompilation.SyntaxTrees.Count()).Select(tree => tree.ToString()));

        AssertContains(generated, "ClampAttribute", "generated aspect manifest / clamp");
        AssertContains(generated, "OffsetReadAttribute", "generated aspect manifest / field get");
        AssertContains(generated, "TrackChangeAttribute", "generated aspect manifest / change");
        AssertContains(generated, "CountCallsAttribute", "generated aspect manifest / method");

        DiagnosticAnalyzer analyzer = new TemplateAnalyzer();
        ImmutableArray<Diagnostic> validDiagnostics = validCompilation
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();

        if (validDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException(
                "Valid fixture produced analyzer errors:" + Environment.NewLine +
                string.Join(Environment.NewLine, validDiagnostics.Select(diagnostic => diagnostic.ToString())));
        }

        CSharpCompilation invalidCompilation = CreateCompilation(InvalidFixture, "UnityMeta.InvalidCompilerFixture");
        ImmutableArray<Diagnostic> invalidDiagnostics = invalidCompilation
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();

        var ids = new HashSet<string>(invalidDiagnostics.Select(diagnostic => diagnostic.Id));
        for (int i = 1; i <= 9; i++)
        {
            string id = "UMETA" + i.ToString("000");
            AssertTrue(ids.Contains(id), "analyzer emits " + id);
        }
    }

    private static CSharpCompilation CreateCompilation(string source, string assemblyName)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp9));
        var references = new List<MetadataReference>();

        string trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        foreach (string path in trusted.Split(Path.PathSeparator))
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(UnityMeta.MetaAspectAttribute).Assembly.Location));

        return CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static byte[] Emit(CSharpCompilation compilation)
    {
        using var stream = new MemoryStream();
        EmitResult result = compilation.Emit(stream);
        if (!result.Success)
        {
            string errors = string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException("Fixture compilation failed:" + Environment.NewLine + errors);
        }

        return stream.ToArray();
    }

    private static byte[] Weave(byte[] original)
    {
        using var input = new MemoryStream(original);
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input);
        var logger = new ThrowingLogger();
        var weaver = new MetaWeaver(logger);

        if (!weaver.Weave(assembly))
        {
            throw new InvalidOperationException("The fixture was not modified by UnityMeta.");
        }

        using var output = new MemoryStream();
        assembly.Write(output);
        return output.ToArray();
    }

    private static void ExecuteRuntimeAssertions(byte[] assemblyBytes)
    {
        Assembly assembly = Assembly.Load(assemblyBytes);
        Type combatType = assembly.GetType("Combat") ?? throw new InvalidOperationException("Combat type missing.");
        object combat = Activator.CreateInstance(combatType) ?? throw new InvalidOperationException("Combat instance missing.");

        MethodInfo setFixed = combatType.GetMethod("SetFixedHp")!;
        FieldInfo fixedHp = combatType.GetField("FixedHp")!;
        FieldInfo changeCount = combatType.GetField("ChangeCount")!;

        setFixed.Invoke(combat, new object[] { 500 });
        AssertEqual(100, (int)fixedHp.GetValue(combat)!, "constant clamp max");
        AssertEqual(1, (int)changeCount.GetValue(combat)!, "change template executes after final transformed write");
        AssertEqual(0, (int)combatType.GetField("LastOldValue")!.GetValue(combat)!, "change old value");
        AssertEqual(100, (int)combatType.GetField("LastNewValue")!.GetValue(combat)!, "change new value");
        AssertEqual("fixed-hp", (string)combatType.GetField("LastTag")!.GetValue(combat)!, "named aspect argument");

        setFixed.Invoke(combat, new object[] { 500 });
        AssertEqual(1, (int)changeCount.GetValue(combat)!, "unchanged final value does not notify");

        setFixed.Invoke(combat, new object[] { -20 });
        AssertEqual(0, (int)fixedHp.GetValue(combat)!, "constant clamp min");
        AssertEqual(2, (int)changeCount.GetValue(combat)!, "second real change notifies");
        AssertEqual(100, (int)combatType.GetField("LastOldValue")!.GetValue(combat)!, "second change old value");
        AssertEqual(0, (int)combatType.GetField("LastNewValue")!.GetValue(combat)!, "second change new value");

        FieldInfo maxHp = combatType.GetField("MaxHp")!;
        FieldInfo hp = combatType.GetField("Hp")!;
        MethodInfo setHp = combatType.GetMethod("SetHp")!;
        maxHp.SetValue(combat, 45);
        setHp.Invoke(combat, new object[] { 80 });
        AssertEqual(45, (int)hp.GetValue(combat)!, "dynamic sibling-field clamp");

        MethodInfo setDisplayed = combatType.GetMethod("SetDisplayed")!;
        MethodInfo readDisplayed = combatType.GetMethod("ReadDisplayed")!;
        FieldInfo displayed = combatType.GetField("Displayed")!;
        setDisplayed.Invoke(combat, new object[] { 10 });
        AssertEqual(10, (int)displayed.GetValue(combat)!, "field-get aspect preserves raw storage");
        AssertEqual(15, (int)readDisplayed.Invoke(combat, Array.Empty<object>())!, "field-get aspect transforms direct reads");

        MethodInfo attack = combatType.GetMethod("Attack")!;
        object? attackResult = attack.Invoke(combat, new object[] { 7 });
        AssertEqual(14, (int)attackResult!, "method return value");
        AssertEqual(1, (int)combatType.GetField("BeforeCount")!.GetValue(combat)!, "before template");
        AssertEqual(1, (int)combatType.GetField("AfterCount")!.GetValue(combat)!, "after template");
        AssertEqual(14, (int)combatType.GetField("LastReturnValue")!.GetValue(combat)!, "after return-value binding");
        AssertEqual("Combat:attack", (string)combatType.GetField("LastMetadata")!.GetValue(combat)!, "Type + named argument metadata");

        MethodInfo stressShortBranch = combatType.GetMethod("StressShortBranch")!;
        stressShortBranch.Invoke(combat, new object[] { true });
        stressShortBranch.Invoke(combat, new object[] { false });
        AssertEqual(100, (int)fixedHp.GetValue(combat)!, "short branch remains valid after large weaving expansion");

        MethodInfo stressStateMachine = combatType.GetMethod("StressStateMachine")!;
        var stateMachineTask = (System.Threading.Tasks.Task<int>)stressStateMachine.Invoke(combat, new object[] { false })!;
        AssertEqual(100, stateMachineTask.GetAwaiter().GetResult(), "state-machine MoveNext remains valid after weaving");
    }

    private static void AssertEqual(int expected, int actual, string name)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(name + ": expected " + expected + ", got " + actual + ".");
        }
    }

    private static void AssertEqual(string expected, string actual, string name)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(name + ": expected '" + expected + "', got '" + actual + "'.");
        }
    }

    private static void AssertContains(string value, string expected, string name)
    {
        if (value.IndexOf(expected, StringComparison.Ordinal) < 0)
        {
            throw new InvalidOperationException(name + ": expected generated text to contain '" + expected + "'.");
        }
    }

    private static void AssertTrue(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name + ": assertion failed.");
        }
    }

    private sealed class ThrowingLogger : IMetaLogger
    {
        public void Warning(string message)
        {
            Console.Error.WriteLine("warning: " + message);
        }

        public void Error(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}
