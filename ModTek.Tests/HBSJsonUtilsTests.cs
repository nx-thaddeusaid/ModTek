using System;
using System.IO;
using ModTek.Util;
using Xunit;

namespace ModTek.Tests;

// Regression gate (T1D in testing-plan.md).
//
// Wave 3 (Corvus + Flint, commit 07bc917 / 15efd77) replaced
// HBSJsonUtils.ParseGameJSONFile's `ReadAllText + JObject.Parse` with a streaming
// `JObject.Load(JsonTextReader)`. Allocation dropped 24–32% across all file sizes
// and Gen2 collections on a 378 KB file went from 179 to 0 per parse.
//
// This test asserts the streaming behaviour by measuring allocation on a
// known-size large JSON file. Empirical measurements on the test fixture
// (large_test.json, 598 KB) on Linux net8.0:
//
//   streaming   :  7,940 KB allocated  (12.97x file size)
//   ReadAllText : 10,403 KB allocated  (17.00x file size)
//
// The 15x ceiling below sits cleanly between them — a silent revert of the
// fast path will trip it.
public class HBSJsonUtilsTests
{
    private static readonly string TestDataDir =
        Path.Combine(AppContext.BaseDirectory, "TestData");

    [Theory]
    [InlineData("small_test.json")]
    [InlineData("medium_test.json")]
    [InlineData("large_test.json")]
    public void ParseGameJSONFile_ProducesNonNullJObject(string filename)
    {
        var path = Path.Combine(TestDataDir, filename);
        Assert.True(File.Exists(path), $"Test fixture missing: {path}");

        var result = HBSJsonUtils.ParseGameJSONFile(path);

        Assert.NotNull(result);
        Assert.NotNull(result["Description"]);
        Assert.Equal("chassisdef_test_large", (string)result["Description"]["Id"]);
    }

    [Fact]
    public void ParseGameJSONFile_LargeFile_StaysBelowReadAllTextCeiling()
    {
        var path = Path.Combine(TestDataDir, "large_test.json");
        Assert.True(File.Exists(path), $"Test fixture missing: {path}");

        var fileSize = new FileInfo(path).Length;

        // Warm-up: pay one-time JIT, regex compile, and JIT'd dispatch costs.
        HBSJsonUtils.ParseGameJSONFile(path);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Measure total allocations across one call. GetTotalAllocatedBytes is
        // cumulative and not affected by GC behaviour — closer to what
        // BenchmarkDotNet reports as `Allocated` than GetTotalMemory would be.
        var before = GC.GetTotalAllocatedBytes(precise: true);
        HBSJsonUtils.ParseGameJSONFile(path);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;

        // 15x ceiling sits between streaming (≈13x) and ReadAllText (≈17x) on
        // this fixture; see class header for the empirical measurements.
        var ceiling = fileSize * 15;

        Assert.True(
            allocated < ceiling,
            $"ParseGameJSONFile allocated {allocated / 1024} KB for a "
            + $"{fileSize / 1024} KB input ({(double)allocated / fileSize:F2}x). "
            + $"Expected < {ceiling / 1024} KB (15x). Likely cause: the streaming "
            + "fast path in HBSJsonUtils.ParseGameJSONFile has been reverted to "
            + "the ReadAllText pipeline (see Wave 3 benchmark in unified-roadmap.md)."
        );
    }
}
