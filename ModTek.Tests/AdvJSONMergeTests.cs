using ModTek.Features.AdvJSONMerge;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ModTek.Tests;

public class AdvJSONMergeTests
{
    private static JObject Root(string json) => JObject.Parse(json);

    // ── Remove ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Remove_Property_RemovesItFromObject()
    {
        var root = Root("""{"a": 1, "b": 2}""");
        MergeApplicator.Apply(MergeAction.Remove, root["a"], null);
        Assert.Null(root["a"]);
        Assert.NotNull(root["b"]);
    }

    [Fact]
    public void Remove_ArrayElement_RemovesIt()
    {
        var root = Root("""{"arr": [1, 2, 3]}""");
        MergeApplicator.Apply(MergeAction.Remove, root["arr"]![1], null);
        Assert.Equal(2, ((JArray)root["arr"]!).Count);
        Assert.Equal(1, root["arr"]![0]!.Value<int>());
        Assert.Equal(3, root["arr"]![1]!.Value<int>());
    }

    // ── Replace ────────────────────────────────────────────────────────────────

    [Fact]
    public void Replace_ScalarValue_ReplacesIt()
    {
        var root = Root("""{"x": 10}""");
        MergeApplicator.Apply(MergeAction.Replace, root["x"], JToken.FromObject(99));
        Assert.Equal(99, root["x"]!.Value<int>());
    }

    [Fact]
    public void Replace_WithObject_ReplacesEntireValue()
    {
        var root = Root("""{"inner": {"a": 1}}""");
        MergeApplicator.Apply(MergeAction.Replace, root["inner"], JObject.Parse("""{"b": 2}"""));
        Assert.Null(root["inner"]!["a"]);
        Assert.Equal(2, root["inner"]!["b"]!.Value<int>());
    }

    // ── ArrayAdd ──────────────────────────────────────────────────────────────

    [Fact]
    public void ArrayAdd_AppendsToEnd()
    {
        var root = Root("""{"arr": [1, 2]}""");
        MergeApplicator.Apply(MergeAction.ArrayAdd, root["arr"], JToken.FromObject(3));
        Assert.Equal(3, ((JArray)root["arr"]!).Count);
        Assert.Equal(3, root["arr"]![2]!.Value<int>());
    }

    [Fact]
    public void ArrayAdd_ToNonArray_Throws()
    {
        var root = Root("""{"x": 1}""");
        Assert.Throws<System.Exception>(() =>
            MergeApplicator.Apply(MergeAction.ArrayAdd, root["x"], JToken.FromObject(2)));
    }

    // ── ArrayAddAfter / ArrayAddBefore ─────────────────────────────────────────

    [Fact]
    public void ArrayAddAfter_InsertsAfterTarget()
    {
        var root = Root("""{"arr": [1, 3]}""");
        MergeApplicator.Apply(MergeAction.ArrayAddAfter, root["arr"]![0], JToken.FromObject(2));
        Assert.Equal(3, ((JArray)root["arr"]!).Count);
        Assert.Equal(2, root["arr"]![1]!.Value<int>());
    }

    [Fact]
    public void ArrayAddBefore_InsertsBeforeTarget()
    {
        var root = Root("""{"arr": [1, 3]}""");
        MergeApplicator.Apply(MergeAction.ArrayAddBefore, root["arr"]![1], JToken.FromObject(2));
        Assert.Equal(3, ((JArray)root["arr"]!).Count);
        Assert.Equal(2, root["arr"]![1]!.Value<int>());
        Assert.Equal(3, root["arr"]![2]!.Value<int>());
    }

    // ── ObjectMerge ────────────────────────────────────────────────────────────

    [Fact]
    public void ObjectMerge_AddsNewProperties()
    {
        var root = Root("""{"obj": {"a": 1}}""");
        MergeApplicator.Apply(MergeAction.ObjectMerge, root["obj"], JObject.Parse("""{"b": 2}"""));
        Assert.Equal(1, root["obj"]!["a"]!.Value<int>());
        Assert.Equal(2, root["obj"]!["b"]!.Value<int>());
    }

    [Fact]
    public void ObjectMerge_OverwritesExistingProperty()
    {
        var root = Root("""{"obj": {"a": 1}}""");
        MergeApplicator.Apply(MergeAction.ObjectMerge, root["obj"], JObject.Parse("""{"a": 99}"""));
        Assert.Equal(99, root["obj"]!["a"]!.Value<int>());
    }

    [Fact]
    public void ObjectMerge_ReplacesArrayProperties()
    {
        // MergeArrayHandling.Replace: incoming array replaces existing
        var root = Root("""{"obj": {"arr": [1, 2]}}""");
        MergeApplicator.Apply(MergeAction.ObjectMerge, root["obj"], JObject.Parse("""{"arr": [3]}"""));
        Assert.Single((JArray)root["obj"]!["arr"]!);
        Assert.Equal(3, root["obj"]!["arr"]![0]!.Value<int>());
    }

    [Fact]
    public void ObjectMerge_OnNonObject_Throws()
    {
        var root = Root("""{"x": 1}""");
        Assert.Throws<System.Exception>(() =>
            MergeApplicator.Apply(MergeAction.ObjectMerge, root["x"], JObject.Parse("{}")));
    }

    // ── ArrayConcat ────────────────────────────────────────────────────────────

    [Fact]
    public void ArrayConcat_CombinesBothArrays()
    {
        var root = Root("""{"arr": [1, 2]}""");
        MergeApplicator.Apply(MergeAction.ArrayConcat, root["arr"], JArray.Parse("[3, 4]"));
        Assert.Equal(4, ((JArray)root["arr"]!).Count);
        Assert.Equal(3, root["arr"]![2]!.Value<int>());
        Assert.Equal(4, root["arr"]![3]!.Value<int>());
    }

    [Fact]
    public void ArrayConcat_OnNonArray_Throws()
    {
        var root = Root("""{"x": 1}""");
        Assert.Throws<System.Exception>(() =>
            MergeApplicator.Apply(MergeAction.ArrayConcat, root["x"], JArray.Parse("[]")));
    }

    // ── GetDefaultValue ────────────────────────────────────────────────────────

    [Fact]
    public void GetDefaultValue_ArrayAdd_ReturnsEmptyArray()
    {
        var val = MergeApplicator.GetDefaultValue(MergeAction.ArrayAdd);
        Assert.IsType<JArray>(val);
    }

    [Fact]
    public void GetDefaultValue_ObjectMerge_ReturnsEmptyObject()
    {
        var val = MergeApplicator.GetDefaultValue(MergeAction.ObjectMerge);
        Assert.IsType<JObject>(val);
    }

    [Fact]
    public void GetDefaultValue_Remove_Throws()
    {
        Assert.Throws<System.Exception>(() => MergeApplicator.GetDefaultValue(MergeAction.Remove));
    }
}
