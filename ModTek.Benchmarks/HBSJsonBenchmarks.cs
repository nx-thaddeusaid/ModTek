using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ModTek.Benchmarks;

/// <summary>
/// Streaming JSON parse (JObject.Load + JsonTextReader) vs ReadAllText + JObject.Parse.
/// The streaming path is the current fast path in HBSJsonUtils.ParseGameJSONFile.
/// Files: small mechdef ~7KB, medium MechEngineer settings ~40KB, large localization ~823KB.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "RatioSD")]
public class HBSJsonBenchmarks
{
    [Params(
        "/mount/ssd2/work/roguetech-mods/RogueTech/Core/Solaris7/mech/mechdef_scorpion_SCP-S7.json",
        "/mount/ssd2/work/roguetech-mods/RogueTech/Core/MechEngineer/Settings.json",
        "/mount/ssd2/work/roguetech-mods/RogueTech/Core/RogueTechCore/Localization.json")]
    public string FilePath;

    [Benchmark(Baseline = true)]
    public JObject ReadAllTextThenParse()
    {
        var content = File.ReadAllText(FilePath);
        return JObject.Parse(content);
    }

    [Benchmark]
    public JObject StreamingLoad()
    {
        using var stream = File.OpenRead(FilePath);
        using var textReader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(textReader)
        {
            DateParseHandling = DateParseHandling.None,
        };
        return JObject.Load(jsonReader);
    }
}
