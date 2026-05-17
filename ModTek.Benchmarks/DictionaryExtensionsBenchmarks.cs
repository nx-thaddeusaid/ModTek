using BenchmarkDotNet.Attributes;
using ModTek.Common.Utils;

namespace ModTek.Benchmarks;

[MemoryDiagnoser]
public class DictionaryExtensionsBenchmarks
{
    private Dictionary<string, List<string>> _dict = null!;

    [Params(100, 1000, 10000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _dict = new Dictionary<string, List<string>>(Size);
        for (var i = 0; i < Size; i++)
            _dict[$"key_{i}"] = ["value"];
    }

    [Benchmark(Description = "GetOrCreate — hot (key exists)")]
    public List<string> GetOrCreate_Hot() =>
        _dict.GetOrCreate("key_0");

    [Benchmark(Description = "GetOrCreate — cold (key absent, then evict)")]
    public int GetOrCreate_Cold()
    {
        var key = $"__bench_{Guid.NewGuid():N}";
        var result = _dict.GetOrCreate(key);
        _dict.Remove(key);
        return result.Count;
    }

    [Benchmark(Description = "GetOrCreate — scan last key (worst lookup position)")]
    public List<string> GetOrCreate_LastKey() =>
        _dict.GetOrCreate($"key_{Size - 1}");
}
