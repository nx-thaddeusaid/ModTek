using System.Collections.Generic;
using System.Linq;
using ModTek;
using ModTek.Features.AdvJSONMerge;
using ModTek.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ModTek.Tests;

/// <summary>
/// Integration tests exercising realistic mod loading scenarios:
/// LoadOrder + AdvJSONMerge working together as ModTek does at runtime.
/// No BattleTech game types are needed — all game data is represented as JObject/JArray.
/// </summary>
public class ModLoadingIntegrationTests
{
    // ── helpers ────────────────────────────────────────────────────────────────

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

    // ── LoadOrder integration scenarios ───────────────────────────────────────

    [Fact]
    public void LargeModGraph_AllIndependent_AllLoaded()
    {
        var mods = Dict(
            Mod("Mod01"), Mod("Mod02"), Mod("Mod03"), Mod("Mod04"), Mod("Mod05"),
            Mod("Mod06"), Mod("Mod07"), Mod("Mod08"), Mod("Mod09"), Mod("Mod10")
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(10, order.Count);
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void RealWorldStyleGraph_LinearChain_CorrectOrder()
    {
        // RT → CustomAmmo → CAB → Lances — a common real-world dependency pattern.
        var mods = Dict(
            Mod("CAB"),
            Mod("CustomAmmoCategories", deps: new[] { "CAB" }),
            Mod("RogueTech", deps: new[] { "CAB", "CustomAmmoCategories" }),
            Mod("Lances", deps: new[] { "RogueTech" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Equal(new[] { "CAB", "CustomAmmoCategories", "RogueTech", "Lances" }, order.ToArray());
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void ThreeWayCircularDep_AllExcluded()
    {
        var mods = Dict(
            Mod("ModA", deps: new[] { "ModC" }),
            Mod("ModB", deps: new[] { "ModA" }),
            Mod("ModC", deps: new[] { "ModB" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Empty(order);
        Assert.Equal(3, notLoaded.Count);
    }

    [Fact]
    public void PartialCircle_IndependentModsStillLoad()
    {
        var mods = Dict(
            Mod("ModA", deps: new[] { "ModB" }),
            Mod("ModB", deps: new[] { "ModA" }), // A-B cycle
            Mod("ModC"),
            Mod("ModD", deps: new[] { "ModC" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Contains("ModC", order);
        Assert.Contains("ModD", order);
        Assert.DoesNotContain("ModA", order);
        Assert.DoesNotContain("ModB", order);
        Assert.Equal(2, notLoaded.Count);
    }

    [Fact]
    public void MultipleConflicts_ConflictingModsExcluded()
    {
        var mods = Dict(
            Mod("BaseWeapons"),
            Mod("ExtendedWeapons", conflicts: new[] { "BaseWeapons" }),
            Mod("AmmoMod", deps: new[] { "BaseWeapons" }),
            Mod("UIMod")
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        // ExtendedWeapons conflicts with BaseWeapons → ExtendedWeapons excluded
        Assert.DoesNotContain("ExtendedWeapons", order);
        Assert.Contains("BaseWeapons", order);
        Assert.Contains("AmmoMod", order);
        Assert.Contains("UIMod", order);
    }

    [Fact]
    public void OptionalDepsWithMixedPresence_CorrectOrdering()
    {
        var mods = Dict(
            Mod("CoreLib"),
            Mod("WeaponsMod", optional: new[] { "CoreLib", "MissingMod" }),
            Mod("UIMod", optional: new[] { "WeaponsMod" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        // CoreLib present → WeaponsMod loads after CoreLib
        // MissingMod absent → ignored
        // WeaponsMod present → UIMod loads after WeaponsMod
        Assert.Equal(new[] { "CoreLib", "WeaponsMod", "UIMod" }, order.ToArray());
        Assert.Empty(notLoaded);
    }

    [Fact]
    public void MissingDep_TransitiveFail_ChainExcluded()
    {
        var mods = Dict(
            Mod("ModB", deps: new[] { "ModA" }),       // ModA missing
            Mod("ModC", deps: new[] { "ModB" }),        // depends on excluded ModB
            Mod("ModD")
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.DoesNotContain("ModB", order);
        Assert.DoesNotContain("ModC", order);
        Assert.Contains("ModD", order);
        // ModB and ModC both fail to load
        Assert.Equal(2, notLoaded.Count);
    }

    // ── LoadOrder + AdvJSONMerge pipeline integration ─────────────────────────

    [Fact]
    public void PatchesAppliedInLoadOrder_LaterPatchWins()
    {
        // Two mods both patch the same field; the one that loads second wins.
        var mods = Dict(
            Mod("BaseMod"),
            Mod("OverrideMod", deps: new[] { "BaseMod" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out _);
        Assert.Equal(new[] { "BaseMod", "OverrideMod" }, order.ToArray());

        var baseMechdef = """{"Tonnage": 50, "Name": "Atlas"}""";

        // BaseMod sets Tonnage to 60; OverrideMod then sets it to 75.
        var baseModPatch = """{"Tonnage": 60}""";
        var overrideModPatch = """{"Tonnage": 75}""";

        var patches = new Dictionary<string, string>
        {
            ["BaseMod"] = baseModPatch,
            ["OverrideMod"] = overrideModPatch,
        };

        var target = JObject.Parse(baseMechdef);
        foreach (var modName in order)
        {
            if (patches.TryGetValue(modName, out var patch))
                target.Merge(JObject.Parse(patch), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
        }

        Assert.Equal(75, target["Tonnage"]!.Value<int>());
    }

    [Fact]
    public void AdvJSONMerge_ArrayAdd_RealWeaponList()
    {
        var baseMechdef = """
            {
                "Description": {"Id": "mechdef_atlas_AS7-D", "Name": "Atlas AS7-D"},
                "inventory": [
                    {"ComponentDefID": "Weapon_AC20"},
                    {"ComponentDefID": "Weapon_LRM20"}
                ]
            }
            """;

        // A mod adds a new weapon to the inventory array.
        var root = JObject.Parse(baseMechdef);
        var inventory = (JArray)root["inventory"]!;
        MergeApplicator.Apply(MergeAction.ArrayAdd, inventory, JObject.Parse("""{"ComponentDefID": "Weapon_MediumLaser"}"""));

        Assert.Equal(3, inventory.Count);
        Assert.Equal("Weapon_MediumLaser", inventory[2]!["ComponentDefID"]!.Value<string>());
    }

    [Fact]
    public void AdvJSONMerge_Remove_StripsWeapon()
    {
        var baseMechdef = """
            {
                "inventory": [
                    {"ComponentDefID": "Weapon_AC20"},
                    {"ComponentDefID": "Weapon_LRM20"}
                ]
            }
            """;

        var root = JObject.Parse(baseMechdef);
        var lrm = root.SelectToken("inventory[1]")!;
        MergeApplicator.Apply(MergeAction.Remove, lrm, null);

        var inventory = (JArray)root["inventory"]!;
        Assert.Single(inventory);
        Assert.Equal("Weapon_AC20", inventory[0]!["ComponentDefID"]!.Value<string>());
    }

    [Fact]
    public void AdvJSONMerge_ObjectMerge_NestedDescriptionUpdate()
    {
        var baseJson = """
            {
                "Description": {
                    "Id": "chassisdef_atlas",
                    "Name": "Atlas",
                    "Details": "Original details."
                }
            }
            """;

        var root = JObject.Parse(baseJson);
        var desc = root["Description"]!;
        MergeApplicator.Apply(
            MergeAction.ObjectMerge,
            desc,
            JObject.Parse("""{"Details": "Updated details.", "UIName": "Atlas Prime"}""")
        );

        Assert.Equal("Updated details.", root["Description"]!["Details"]!.Value<string>());
        Assert.Equal("Atlas Prime", root["Description"]!["UIName"]!.Value<string>());
        Assert.Equal("chassisdef_atlas", root["Description"]!["Id"]!.Value<string>()); // preserved
    }

    [Fact]
    public void AdvJSONMerge_ArrayConcat_MergesTwoInventories()
    {
        var baseJson = """{"slots": [1, 2, 3]}""";
        var root = JObject.Parse(baseJson);
        var slots = root["slots"]!;
        MergeApplicator.Apply(MergeAction.ArrayConcat, slots, JArray.Parse("[4, 5]"));

        Assert.Equal(5, ((JArray)root["slots"]!).Count);
    }

    [Fact]
    public void RogueTechStylePipeline_ThreeModsApplyInOrder()
    {
        // Simulate a realistic 3-mod patching scenario:
        // CAB provides a base weapon, WeaponMod rebalances it, NightmareMod further tweaks it.
        var mods = Dict(
            Mod("CAB"),
            Mod("WeaponMod", deps: new[] { "CAB" }),
            Mod("NightmareMod", deps: new[] { "WeaponMod" })
        );
        var order = LoadOrder.CreateLoadOrder(mods, out var notLoaded);
        Assert.Empty(notLoaded);
        Assert.Equal(new[] { "CAB", "WeaponMod", "NightmareMod" }, order.ToArray());

        var baseWeapon = """{"Damage": 40, "MinRange": 0, "MaxRange": 450, "HeatGenerated": 35}""";
        var weaponModPatch = """{"Damage": 45, "HeatGenerated": 30}""";
        var nightmarePatch = """{"Damage": 55, "MaxRange": 480}""";

        var patches = new[] { weaponModPatch, nightmarePatch };
        var target = JObject.Parse(baseWeapon);
        foreach (var patch in patches)
            target.Merge(JObject.Parse(patch), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

        Assert.Equal(55, target["Damage"]!.Value<int>());
        Assert.Equal(480, target["MaxRange"]!.Value<int>());
        Assert.Equal(0, target["MinRange"]!.Value<int>());   // untouched
        Assert.Equal(30, target["HeatGenerated"]!.Value<int>()); // WeaponMod value preserved
    }
}
