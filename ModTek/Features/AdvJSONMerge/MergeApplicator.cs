using System;
using Newtonsoft.Json.Linq;

namespace ModTek.Features.AdvJSONMerge;

internal static class MergeApplicator
{
    internal static void Apply(MergeAction action, JToken jToken, JToken value)
    {
        switch (action)
        {
            case MergeAction.Remove:
                {
                    if (jToken.Parent is JProperty)
                    {
                        jToken.Parent.Remove();
                    }
                    else
                    {
                        jToken.Remove();
                    }

                    break;
                }
            case MergeAction.Replace:
                {
                    jToken.Replace(value);
                    break;
                }
            case MergeAction.ArrayAdd:
                {
                    if (jToken is not JArray jArray)
                    {
                        throw new Exception("JSONPath needs to point to an array");
                    }

                    jArray.Add(value);
                    break;
                }
            case MergeAction.ArrayAddAfter:
                {
                    jToken.AddAfterSelf(value);
                    break;
                }
            case MergeAction.ArrayAddBefore:
                {
                    jToken.AddBeforeSelf(value);
                    break;
                }
            case MergeAction.ObjectMerge:
                {
                    if (jToken is not JObject jObject1 || value is not JObject jObject2)
                    {
                        throw new Exception("JSONPath has to point to an object and Value has to be an object");
                    }

                    jObject1.Merge(jObject2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                    break;
                }
            case MergeAction.ArrayConcat:
                {
                    if (jToken is not JArray jArray1 || value is not JArray jArray2)
                    {
                        throw new Exception("JSONPath has to point to an array and Value has to be an array");
                    }

                    jArray1.Merge(jArray2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                    break;
                }
            default:
                {
                    throw new Exception("Unhandled action");
                }
        }
    }

    internal static JToken GetDefaultValue(MergeAction action)
    {
        switch (action)
        {
            case MergeAction.ArrayAdd:
            case MergeAction.ArrayConcat:
                return new JArray();
            case MergeAction.Replace:
            case MergeAction.ObjectMerge:
                return new JObject();
            default:
                throw new Exception($"AutoCreateProperty: The merge action is not supported for action {action}");
        }
    }
}
