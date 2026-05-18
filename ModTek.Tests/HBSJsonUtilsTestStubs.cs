// Minimal stubs to let HBSJsonUtils.cs compile in the test assembly.
// HBSJsonUtils references ModTek.Log (a NullableLogger-based static class)
// and HBS.Util.JSONSerializationUtility (from BattleTech's Assembly-CSharp.dll).
// Neither is available in this xUnit project. The fast-path tested here never
// invokes either at runtime (no logging is emitted on success, and the HBS
// comment-stripping fallback is only reached for HBS-format JSON which the
// test fixtures deliberately don't contain).
//
// Same pattern as ModDefExStub.cs.

namespace ModTek
{
    internal static class Log
    {
        internal static readonly StubLogger Main = new();

        // Signature mirrors the real Log.LogIf extension method so HBSJsonUtils.cs
        // compiles unchanged. Parameters are intentionally unused — the discard
        // assignments below silence IDE0060 without altering behaviour.
        internal static void LogIf(this StubLevel @this, bool condition, string message)
        {
            _ = @this; _ = condition; _ = message;
        }
    }

    internal sealed class StubLogger
    {
        internal StubLevel Error => null;
        internal StubLevel Info => null;
    }

    internal sealed class StubLevel
    {
        internal void Log(string message) { _ = message; }
    }
}

namespace HBS.Util
{
    internal static class JSONSerializationUtility
    {
        // Only invoked by the HBSJsonUtils fallback path (HBS-format JSON with
        // comments). The perf test uses clean JSON that streams successfully on
        // the first attempt, so this is never called at runtime.
        internal static string StripHBSCommentsFromJSON(string content) => content;
    }
}
