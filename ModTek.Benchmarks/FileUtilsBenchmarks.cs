using BenchmarkDotNet.Attributes;
using ModTek.Common.Utils;

namespace ModTek.Benchmarks;

[MemoryDiagnoser]
public class FileUtilsBenchmarks
{
    private static readonly string BasePath = AppContext.BaseDirectory;
    private static readonly string AbsolutePath = Path.Combine(AppContext.BaseDirectory, "sub", "dir", "file.dll");
    private static readonly string ShallowPath = Path.Combine(AppContext.BaseDirectory, "file.dll");
    private static readonly string[] Deny = [".DS_STORE", "normal.json", "~notes", "img.nomedia"];

    [Benchmark]
    public string GetRealRelativePath_Deep() =>
        FileUtils.GetRealRelativePath(AbsolutePath, BasePath);

    [Benchmark]
    public string GetRealRelativePath_Shallow() =>
        FileUtils.GetRealRelativePath(ShallowPath, BasePath);

    [Benchmark]
    public string GetRealRelativePath_AlreadyRelative() =>
        FileUtils.GetRealRelativePath("relative/path/file.json", BasePath);

    [Benchmark]
    public bool FileIsOnDenyList_Match() =>
        FileUtils.FileIsOnDenyList("archive.nomedia");

    [Benchmark]
    public bool FileIsOnDenyList_NoMatch() =>
        FileUtils.FileIsOnDenyList("normal.json");

    [Benchmark]
    [Arguments(1)]
    [Arguments(4)]
    public int FileIsOnDenyList_Batch(int n)
    {
        var hits = 0;
        foreach (var p in Deny.Take(n))
            if (FileUtils.FileIsOnDenyList(p)) hits++;
        return hits;
    }
}
