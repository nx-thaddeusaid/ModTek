// Minimal stub of ModDefEx for tests that include LoadOrder.cs.
// LoadOrder only uses Name, DependsOn, ConflictsWith, OptionallyDependsOn,
// and the two Calc* methods — no BattleTech game types needed.
using System.Collections.Generic;
using System.Linq;

namespace ModTek;

internal class ModDefEx
{
    public string Name { get; set; }
    public HashSet<string> DependsOn { get; set; } = new();
    public HashSet<string> ConflictsWith { get; set; } = new();
    public HashSet<string> OptionallyDependsOn { get; set; } = new();

    internal List<string> CalcMissingDependsOn(IEnumerable<string> loaded)
        => DependsOn.Except(loaded).ToList();

    internal List<string> CalcConflicts(IEnumerable<string> otherMods)
        => ConflictsWith.Intersect(otherMods).ToList();
}
