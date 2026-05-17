using System.Collections.Generic;
using ModTek.Common.Utils;
using Xunit;

namespace ModTek.Tests;

public class DictionaryExtensionsTests
{
    [Fact]
    public void GetOrCreate_KeyMissing_CreatesAndReturnsNewValue()
    {
        var dict = new Dictionary<string, List<int>>();
        var value = dict.GetOrCreate("key");
        Assert.NotNull(value);
        Assert.Same(dict["key"], value);
    }

    [Fact]
    public void GetOrCreate_KeyExists_ReturnsExistingValue()
    {
        var dict = new Dictionary<string, List<int>>();
        var existing = new List<int> { 1, 2, 3 };
        dict["key"] = existing;

        var result = dict.GetOrCreate("key");
        Assert.Same(existing, result);
    }

    [Fact]
    public void GetOrCreate_KeyMissing_StoredInDict()
    {
        var dict = new Dictionary<string, List<int>>();
        var value = dict.GetOrCreate("key");
        value.Add(42);

        Assert.Equal(42, dict["key"][0]);
    }
}
