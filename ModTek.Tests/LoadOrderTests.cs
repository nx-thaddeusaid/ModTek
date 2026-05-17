using System.Collections.Generic;
using ModTek;
using ModTek.Util;
using Xunit;

namespace ModTek.Tests;

public class LoadOrderTests
{
    // ── SortPaths ──────────────────────────────────────────────────────────────

    [Fact]
    public void SortPaths_EmptyArray_NoOp()
    {
        var paths = new string[0];
        LoadOrder.SortPaths(paths);
        Assert.Empty(paths);
    }

    [Fact]
    public void SortPaths_SinglePath_Unchanged()
    {
        var paths = new[] { "Mods/ModA/mod.json" };
        LoadOrder.SortPaths(paths);
        Assert.Equal("Mods/ModA/mod.json", paths[0]);
    }

    [Fact]
    public void SortPaths_SameDepth_SortedLexicographically()
    {
        var paths = new[] { "Mods/ModZ/mod.json", "Mods/ModA/mod.json" };
        LoadOrder.SortPaths(paths);
        Assert.Equal("Mods/ModA/mod.json", paths[0]);
        Assert.Equal("Mods/ModZ/mod.json", paths[1]);
    }

    [Fact]
    public void SortPaths_ShallowerPathsFirst()
    {
        var paths = new[]
        {
            "Mods/MyMods/ModA/mod.json",
            "Mods/ModA/mod.json",
        };
        LoadOrder.SortPaths(paths);
        Assert.Equal("Mods/ModA/mod.json", paths[0]);
        Assert.Equal("Mods/MyMods/ModA/mod.json", paths[1]);
    }

    [Fact]
    public void SortPaths_MixedDepths_ShallowBeforeDeep()
    {
        var paths = new[]
        {
            "Mods/Deep/Nested/ModC/mod.json",
            "Mods/ModA/mod.json",
            "Mods/Sub/ModB/mod.json",
        };
        LoadOrder.SortPaths(paths);
        // depth 2 < depth 3 < depth 4 (counting separators)
        Assert.Equal("Mods/ModA/mod.json", paths[0]);
        Assert.Equal("Mods/Sub/ModB/mod.json", paths[1]);
        Assert.Equal("Mods/Deep/Nested/ModC/mod.json", paths[2]);
    }

    [Fact]
    public void SortPaths_SameDepth_TieBreakLexicographic()
    {
        var paths = new[]
        {
            "Mods/Zeta/mod.json",
            "Mods/Alpha/mod.json",
            "Mods/Mango/mod.json",
        };
        LoadOrder.SortPaths(paths);
        Assert.Equal("Mods/Alpha/mod.json", paths[0]);
        Assert.Equal("Mods/Mango/mod.json", paths[1]);
        Assert.Equal("Mods/Zeta/mod.json", paths[2]);
    }

    // ── CreateLoadOrder ────────────────────────────────────────────────────────

    private static ModDefEx Mod(string name, string[] deps = null, string[] conflicts = null, string[] optional = null)
    {
        var m = new ModDefEx { Name = name };
        if (deps != null) m.DependsOn = new HashSet<string>(deps);
        if (conflicts != null) m.ConflictsWith = new HashSet<string>(conflicts);
        if (optional != null) m.OptionallyDependsOn = new HashSet<string>(optional);
        return m;
    }

    private static Dictionary<string, ModDefEx> Dict(params ModDefEx[] mods)
    {
        var d = new Dictionary<string, ModDefEx>();
        foreach (var m in mods) d[m.Name] = m;
        return d;
    }

    [Fact]
    public void CreateLoadOrder_NoDeps_AllLoaded()
    {
        var mods = Dict(Mod("ModA"), Mod("ModB"), Mod("ModC"));
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(3, order.Count);
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void CreateLoadOrder_SimpleDep_DependencyLoadedFirst()
    {
        var mods = Dict(
            Mod("ModA"),
            Mod("ModB", deps: new[] { "ModA" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(2, order.Count);
        Assert.Empty(notLoaded);
        Assert.Equal("ModA", order[0]);
        Assert.Equal("ModB", order[1]);
    }

    [Fact]
    public void CreateLoadOrder_ChainedDeps_LoadedInOrder()
    {
        var mods = Dict(
            Mod("ModA"),
            Mod("ModB", deps: new[] { "ModA" }),
            Mod("ModC", deps: new[] { "ModB" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(["ModA", "ModB", "ModC"], order);
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void CreateLoadOrder_MissingDep_ModNotLoaded()
    {
        var mods = Dict(
            Mod("ModB", deps: new[] { "ModA" }) // ModA not registered
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Empty(order);
        Assert.Single(notLoaded);
        Assert.Equal("ModB", notLoaded[0].Name);
    }

    [Fact]
    public void CreateLoadOrder_Conflict_ConflictingModNotLoaded()
    {
        var mods = Dict(
            Mod("ModA", conflicts: new[] { "ModB" }),
            Mod("ModB")
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        // ModA declares conflict with ModB — ModA is excluded
        Assert.DoesNotContain("ModA", order);
        Assert.Contains(notLoaded, m => m.Name == "ModA");
    }

    [Fact]
    public void CreateLoadOrder_OptionalDepPresent_PromotedToHard()
    {
        var mods = Dict(
            Mod("ModA"),
            Mod("ModB", optional: new[] { "ModA" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        // Optional ModA is present, so ModB must come after ModA
        Assert.Equal(["ModA", "ModB"], order);
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void CreateLoadOrder_OptionalDepAbsent_IgnoredNotFailed()
    {
        var mods = Dict(
            Mod("ModB", optional: new[] { "ModA" }) // ModA not present
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(["ModB"], order);
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void CreateLoadOrder_CircularDep_BothNotLoaded()
    {
        var mods = Dict(
            Mod("ModA", deps: new[] { "ModB" }),
            Mod("ModB", deps: new[] { "ModA" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Empty(order);
        Assert.Equal(2, notLoaded.Count);
    }

    [Fact]
    public void CreateLoadOrder_DiamondDep_AllLoaded()
    {
        // A <- B, A <- C, B+C <- D
        var mods = Dict(
            Mod("ModA"),
            Mod("ModB", deps: new[] { "ModA" }),
            Mod("ModC", deps: new[] { "ModA" }),
            Mod("ModD", deps: new[] { "ModB", "ModC" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(4, order.Count);
        Assert.Empty(notLoaded);
        Assert.Equal("ModA", order[0]);
        Assert.Equal("ModD", order[3]);
    }

    [Fact]
    public void CreateLoadOrder_EmptyRegistry_EmptyResult()
    {
        var order = LoadOrder.CreateLoadOrder(new Dictionary<string, ModDefEx>(), out var notLoaded);
        Assert.Empty(order);
        Assert.Empty(notLoaded);
    }
}
