using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using UnityMeta.Weaver;

internal static class Program
{
    private const string Fixture = @"
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

public sealed class CountCallsAttribute : MethodAspectAttribute
{
    [BeforeTemplate]
    public static void Before([TargetInstance] Combat target)
    {
        target.BeforeCount++;
    }

    [AfterTemplate]
    public static void After([TargetInstance] Combat target)
    {
        target.AfterCount++;
    }
}

public sealed class Combat
{
    [Clamp(0, 100)]
    public int FixedHp;

    [Clamp(0, nameof(MaxHp))]
    public int Hp;

    public int MaxHp = 100;
    public int BeforeCount;
    public int AfterCount;

    public void SetFixedHp(int value) { FixedHp = value; }
    public void SetHp(int value) { Hp = value; }

    [CountCalls]
    public int Attack(int damage)
    {
        return damage * 2;
    }
}
";

    public static int Main()
    {
        try
        {
            byte[] original = CompileFixture();
            byte[] woven = Weave(original);
            ExecuteAssertions(woven);
            Console.WriteLine("UnityMeta smoke tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static byte[] CompileFixture()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Fixture, new CSharpParseOptions(LanguageVersion.CSharp9));
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

        CSharpCompilation compilation = CSharpCompilation.Create(
            "UnityMeta.SmokeFixture",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            string errors = string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()));
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

    private static void ExecuteAssertions(byte[] assemblyBytes)
    {
        Assembly assembly = Assembly.Load(assemblyBytes);
        Type combatType = assembly.GetType("Combat") ?? throw new InvalidOperationException("Combat type missing.");
        object combat = Activator.CreateInstance(combatType) ?? throw new InvalidOperationException("Combat instance missing.");

        MethodInfo setFixed = combatType.GetMethod("SetFixedHp")!;
        FieldInfo fixedHp = combatType.GetField("FixedHp")!;
        setFixed.Invoke(combat, new object[] { 500 });
        AssertEqual(100, (int)fixedHp.GetValue(combat)!, "constant clamp max");
        setFixed.Invoke(combat, new object[] { -20 });
        AssertEqual(0, (int)fixedHp.GetValue(combat)!, "constant clamp min");

        FieldInfo maxHp = combatType.GetField("MaxHp")!;
        FieldInfo hp = combatType.GetField("Hp")!;
        MethodInfo setHp = combatType.GetMethod("SetHp")!;
        maxHp.SetValue(combat, 45);
        setHp.Invoke(combat, new object[] { 80 });
        AssertEqual(45, (int)hp.GetValue(combat)!, "dynamic sibling-field clamp");

        MethodInfo attack = combatType.GetMethod("Attack")!;
        object? result = attack.Invoke(combat, new object[] { 7 });
        AssertEqual(14, (int)result!, "method return value");
        AssertEqual(1, (int)combatType.GetField("BeforeCount")!.GetValue(combat)!, "before template");
        AssertEqual(1, (int)combatType.GetField("AfterCount")!.GetValue(combat)!, "after template");
    }

    private static void AssertEqual(int expected, int actual, string name)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(name + ": expected " + expected + ", got " + actual + ".");
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
