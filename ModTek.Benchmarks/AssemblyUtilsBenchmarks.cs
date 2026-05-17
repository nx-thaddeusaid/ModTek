using System.Reflection;
using BenchmarkDotNet.Attributes;
using ModTek.Common.Utils;

namespace ModTek.Benchmarks;

[MemoryDiagnoser]
public class AssemblyUtilsBenchmarks
{
    private static readonly MethodInfo StringToString =
        typeof(string).GetMethod("ToString", Type.EmptyTypes)!;

    private static readonly MethodInfo OwnMethod =
        typeof(AssemblyUtilsBenchmarks).GetMethod(nameof(GetFullName_OwnMethod))!;

    private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

    [Benchmark]
    public string GetFullName_SystemType() =>
        AssemblyUtils.GetFullName(StringToString);

    [Benchmark]
    public string GetFullName_OwnMethod() =>
        AssemblyUtils.GetFullName(OwnMethod);

    [Benchmark]
    public string GetAssemblyName_SystemType() =>
        AssemblyUtils.GetAssemblyName(StringToString);

    [Benchmark]
    public string GetLocationOrName_ExecutingAssembly() =>
        AssemblyUtils.GetLocationOrName(ExecutingAssembly);
}
