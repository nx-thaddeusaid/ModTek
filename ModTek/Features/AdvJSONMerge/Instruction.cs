using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Linq.JsonPath;

namespace ModTek.Features.AdvJSONMerge;

internal class Instruction
{
    [JsonProperty(Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    internal MergeAction Action;

    [JsonProperty(Required = Required.Always)]
    public string JSONPath;

    [JsonProperty]
    public JToken Value;

    [JsonProperty]
    public bool AutoCreateProperty;

    public void Process(JObject root)
    {
        var jTokens = GetOrCreateJTokens(root);
        if (jTokens.Count == 0)
        {
            throw new Exception("Did not find anything");
        }

        foreach (var jToken in jTokens)
        {
            MergeApplicator.Apply(Action, jToken, Value);
        }
    }

    private List<JToken> GetOrCreateJTokens(JToken root)
    {
        var jPath = new JPath(JSONPath);

        if (!AutoCreateProperty)
        {
            return JPath.Evaluate(jPath.Filters, root, root, false).ToList();
        }

        var filterCount = jPath.Filters.Count;
        if (filterCount < 1)
        {
            throw new Exception($"{nameof(AutoCreateProperty)}: JSONPath does not contain a field property expression");
        }

        var lastIndex = filterCount - 1;
        FieldFilter fieldFilter;
        {
            fieldFilter = jPath.Filters[lastIndex] as FieldFilter;
            if (fieldFilter?.Name == null)
            {
                throw new Exception($"{nameof(AutoCreateProperty)}: JSONPath does not contain a field property expression at the end");
            }
        }

        IEnumerable<JToken> tmpTokens = new[]
        {
            root
        };
        for (var index = 0; index < lastIndex; index++)
        {
            var filter = jPath.Filters[index];
            tmpTokens = filter.ExecuteFilter(root, tmpTokens, false);
        }

        var defaultValue = GetDefaultValueForAction();

        var tokens = new List<JToken>();
        foreach (var parentToken in tmpTokens)
        {
            var found = fieldFilter.ExecuteFilter(root, new[] { parentToken }, false).SingleOrDefault();
            if (found == null)
            {
                if (parentToken is not JObject parentObject)
                {
                    throw new Exception($"{nameof(AutoCreateProperty)}: The container is not an object and does not accept properties");
                }
                var property = new JProperty(fieldFilter.Name)
                {
                    Value = defaultValue
                };
                parentObject.Add(property);
                tokens.Add(property.Value);
            }
            else
            {
                tokens.Add(found);
            }
        }

        return tokens;
    }

    private JToken GetDefaultValueForAction() => MergeApplicator.GetDefaultValue(Action);
}