using System.Text.Json;
using Crashguard.Engine.Models;

namespace Crashguard.Engine.Services;

/// <summary>
/// Evaluates a <see cref="CanaryTypeRule"/> against a triggered canary's metadata JSON. Rules are
/// independent — a canary can match any number of them — so this only answers "does this one rule
/// match," leaving fan-out across multiple matches to the caller.
/// </summary>
public static class RuleEvaluator
{
    public static bool Matches(Canary canary, CanaryTypeRule rule)
    {
        if (string.IsNullOrWhiteSpace(canary.Metadata)) return false;

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(canary.Metadata);
        }
        catch (JsonException)
        {
            return false;
        }

        // The rule editor's placeholder text suggests paths like "metadata.orderTotal"; canary.Metadata
        // is already just the metadata object, so an optional leading "metadata." is stripped here.
        var path = rule.Field.StartsWith("metadata.", StringComparison.OrdinalIgnoreCase)
            ? rule.Field["metadata.".Length..]
            : rule.Field;

        var found = TryResolvePath(root, path, out var value);

        return rule.Operator switch
        {
            "exists" => found,
            "eq" => found && ValueEquals(value, rule.Value),
            "neq" => !(found && ValueEquals(value, rule.Value)),
            "gt" => found && TryCompareNumeric(value, rule.Value, out var gtCmp) && gtCmp > 0,
            "lt" => found && TryCompareNumeric(value, rule.Value, out var ltCmp) && ltCmp < 0,
            "contains" => found && value.ValueKind == JsonValueKind.String && rule.Value is not null
                && value.GetString()!.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static bool TryResolvePath(JsonElement root, string path, out JsonElement value)
    {
        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                value = default;
                return false;
            }
        }
        value = current;
        return true;
    }

    private static bool ValueEquals(JsonElement value, string? expected)
    {
        if (expected is null) return false;
        return value.ValueKind switch
        {
            JsonValueKind.String => string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Number => value.TryGetDouble(out var n) && double.TryParse(expected, out var e) && n == e,
            JsonValueKind.True => bool.TryParse(expected, out var t) && t,
            JsonValueKind.False => bool.TryParse(expected, out var f) && !f,
            _ => false,
        };
    }

    private static bool TryCompareNumeric(JsonElement value, string? expected, out int comparison)
    {
        comparison = 0;
        if (expected is null || value.ValueKind != JsonValueKind.Number) return false;
        if (!value.TryGetDouble(out var actual) || !double.TryParse(expected, out var target)) return false;
        comparison = actual.CompareTo(target);
        return true;
    }
}
