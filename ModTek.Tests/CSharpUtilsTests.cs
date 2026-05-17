using System.Collections.Generic;
using ModTek.Common.Utils;
using Xunit;

namespace ModTek.Tests;

public class CSharpUtilsTests
{
    // AsTextListLine

    [Fact]
    public void AsTextListLine_FormatsWithPrefix()
    {
        Assert.Equal("\n - item", CSharpUtils.AsTextListLine("item"));
    }

    // AsTextList

    [Fact]
    public void AsTextList_Null_ReturnsNull()
    {
        Assert.Null(CSharpUtils.AsTextList(null));
    }

    [Fact]
    public void AsTextList_Empty_ReturnsEmpty()
    {
        Assert.Equal("", CSharpUtils.AsTextList(new List<string>()));
    }

    [Fact]
    public void AsTextList_SingleItem_ReturnsFormattedItem()
    {
        Assert.Equal("\n - alpha", CSharpUtils.AsTextList(new[] { "alpha" }));
    }

    [Fact]
    public void AsTextList_MultipleItems_ReturnsAllFormatted()
    {
        var result = CSharpUtils.AsTextList(new[] { "a", "b" });
        Assert.Equal("\n - a\n - b", result);
    }

    // Enumerate

    [Fact]
    public void Enumerate_CombinesEnumeratorsInOrder()
    {
        var e1 = Yield(1, 2);
        var e2 = Yield(3, 4);
        var combined = CSharpUtils.Enumerate(e1, e2);

        var result = new List<int>();
        while (combined.MoveNext())
        {
            result.Add(combined.Current);
        }

        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void Enumerate_EmptyEnumerators_ReturnsEmpty()
    {
        var combined = CSharpUtils.Enumerate(Yield<int>(), Yield<int>());
        Assert.False(combined.MoveNext());
    }

    private static IEnumerator<T> Yield<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}
