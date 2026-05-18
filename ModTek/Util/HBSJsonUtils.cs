using System;
using System.IO;
using System.Text.RegularExpressions;
using HBS.Util;
using Newtonsoft.Json.Linq;

namespace ModTek.Util;

internal static class HBSJsonUtils
{
    internal static JObject ParseGameJSONFile(string path, bool log = false)
    {
        // Fast path: stream directly into JObject without allocating the full string.
        // Fails for HBS-format files with // comments or missing commas; fall back to the
        // full string pipeline in that case. The file is only read once per path.
        try
        {
            using var stream = File.OpenRead(path);
            using var textReader = new StreamReader(stream);
            using var jsonReader = new Newtonsoft.Json.JsonTextReader(textReader)
            {
                DateParseHandling = Newtonsoft.Json.DateParseHandling.None,
            };
            return JObject.Load(jsonReader);
        }
        catch
        {
            var content = File.ReadAllText(path);
            return ParseGameJSON(content, log);
        }
    }

    internal static JObject ParseGameJSON(string content, bool log = false)
    {
        Log.Main.Info?.LogIf(log, "content: " + content);

        try
        {
            return JObject.Parse(content);
        }
        catch (Exception)
        {
            // ignored
        }

        var commentsStripped = JSONSerializationUtility.StripHBSCommentsFromJSON(content);
        Log.Main.Info?.LogIf(log, "commentsStripped: " + commentsStripped);

        var commasAdded = FixHBSJsonCommas(commentsStripped);
        Log.Main.Info?.LogIf(log, "commasAdded: " + commasAdded);

        return JObject.Parse(commasAdded);
    }

    private static readonly Regex s_fixMissingCommasInJson = new(
        """(\]|\}|"|[A-Za-z0-9])\s*\n\s*(\[|\{|")""",
        RegexOptions.Singleline | RegexOptions.Compiled
    );
    private static string FixHBSJsonCommas(string json)
    {
        // add missing commas, this only fixes if there is a newline
        return s_fixMissingCommasInJson.Replace(json, "$1,\n$2");
    }
}