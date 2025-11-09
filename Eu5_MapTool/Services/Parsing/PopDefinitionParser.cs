using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Parsing;

/// <summary>
/// Represents a single pop definition.
/// </summary>
public partial class PopDefinition
{
    public string PopType { get; set; } = string.Empty;
    public float Size { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string Religion { get; set; } = string.Empty;

    public string ToPopLine()
    {
        return $"define_pop = {{ type = {PopType} size = {Size:F5} culture = {Culture} religion = {Religion} }}";
    }

    public bool Matches(string popType, string culture, string religion)
    {
        return PopType.Equals(popType, StringComparison.OrdinalIgnoreCase) &&
               Culture.Equals(culture, StringComparison.OrdinalIgnoreCase) &&
               Religion.Equals(religion, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Represents pop definitions for a location.
/// </summary>
public class LocationPopData
{
    public string LocationName { get; set; } = string.Empty;
    public List<PopDefinition> Pops { get; set; } = new();
}

/// <summary>
/// Parser for pop definition files with deeply nested structures.
/// Example format:
/// locations = {
///     stockholm = {
///         define_pop = { type = clergy size = 0.00021 culture = swedish religion = lutheran }
///     }
/// }
/// </summary>
public partial class PopDefinitionParser : IFileParser<Dictionary<string, LocationPopData>>,
                                           IFileWriter<Dictionary<string, LocationPopData>>
{
    [GeneratedRegex(@"type\s*=\s*(\S+)\s+size\s*=\s*(\S+)\s+culture\s*=\s*(\S+)\s+religion\s*=\s*(\S+)")]
    private static partial Regex PopLineRegex();

    [GeneratedRegex(@"^([A-Za-z0-9_]+)\s*=\s*\{$")]
    private static partial Regex LocationHeaderRegex();

    public async Task<Dictionary<string, LocationPopData>> ParseFileAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        return ParseLines(lines);
    }

    public async Task<Dictionary<string, LocationPopData>> ParseFilesAsync(IEnumerable<string> filePaths)
    {
        var result = new Dictionary<string, LocationPopData>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in filePaths)
        {
            var fileData = await ParseFileAsync(filePath);
            foreach (var kvp in fileData)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    public Dictionary<string, LocationPopData> ParseLines(string[] lines)
    {
        var result = new Dictionary<string, LocationPopData>(StringComparer.OrdinalIgnoreCase);
        var lineQueue = new Queue<string>(lines);

        while (lineQueue.Count > 0)
        {
            var line = lineQueue.Dequeue().Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Look for "locations = {" header
            if (line.Contains("locations") && line.Contains('=') && line.Contains('{'))
            {
                ParseLocationsBlock(lineQueue, result);
            }
        }

        return result;
    }

    private void ParseLocationsBlock(Queue<string> lineQueue, Dictionary<string, LocationPopData> result)
    {
        int braceDepth = 1; // We already entered the locations block

        while (lineQueue.Count > 0 && braceDepth > 0)
        {
            var line = lineQueue.Dequeue().Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Track brace depth
            braceDepth += line.Count(c => c == '{');
            braceDepth -= line.Count(c => c == '}');

            // Check if this is a location header (e.g., "stockholm = {")
            var locationMatch = LocationHeaderRegex().Match(line);
            if (locationMatch.Success)
            {
                string locationName = locationMatch.Groups[1].Value;
                var pops = ParseLocationPops(lineQueue);

                result[locationName] = new LocationPopData
                {
                    LocationName = locationName,
                    Pops = pops
                };
            }
        }
    }

    private List<PopDefinition> ParseLocationPops(Queue<string> lineQueue)
    {
        var pops = new List<PopDefinition>();
        int braceDepth = 1; // We already entered the location block

        var currentPopLine = new StringBuilder();

        while (lineQueue.Count > 0 && braceDepth > 0)
        {
            var line = lineQueue.Dequeue().Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            // Track brace depth
            int openBraces = line.Count(c => c == '{');
            int closeBraces = line.Count(c => c == '}');
            braceDepth += openBraces - closeBraces;

            // Accumulate pop definition line
            if (line.Contains("define_pop"))
            {
                currentPopLine.Append(line);
                currentPopLine.Append(' ');

                // Check if pop definition is complete (matching braces)
                string popStr = currentPopLine.ToString();
                int popBraceDepth = popStr.Count(c => c == '{') - popStr.Count(c => c == '}');

                if (popBraceDepth == 0)
                {
                    var pop = ParsePopDefinition(popStr);
                    if (pop != null)
                    {
                        pops.Add(pop);
                    }
                    currentPopLine.Clear();
                }
            }
        }

        return pops;
    }

    private PopDefinition? ParsePopDefinition(string popLine)
    {
        var match = PopLineRegex().Match(popLine);
        if (!match.Success)
            return null;

        return new PopDefinition
        {
            PopType = match.Groups[1].Value,
            Size = float.Parse(match.Groups[2].Value),
            Culture = match.Groups[3].Value,
            Religion = match.Groups[4].Value
        };
    }

    public async Task WriteFileAsync(string filePath, Dictionary<string, LocationPopData> data)
    {
        var lines = SerializeToLines(data);
        await File.WriteAllLinesAsync(filePath, lines);
    }

    public string[] SerializeToLines(Dictionary<string, LocationPopData> data)
    {
        var lines = new List<string>
        {
            "locations = {"
        };

        foreach (var location in data.Values)
        {
            lines.Add($"\t{location.LocationName} = {{");

            foreach (var pop in location.Pops)
            {
                lines.Add($"\t\t{pop.ToPopLine()}");
            }

            lines.Add("\t}");
        }

        lines.Add("}");

        return lines.ToArray();
    }

    /// <summary>
    /// Merge incoming pops into existing location data.
    /// Pops with matching (type, culture, religion) will have their size updated.
    /// </summary>
    public static LocationPopData MergePops(LocationPopData existing, LocationPopData incoming)
    {
        var result = new LocationPopData
        {
            LocationName = existing.LocationName,
            Pops = new List<PopDefinition>(existing.Pops)
        };

        foreach (var incomingPop in incoming.Pops)
        {
            var matchingPop = result.Pops.FirstOrDefault(p =>
                p.Matches(incomingPop.PopType, incomingPop.Culture, incomingPop.Religion));

            if (matchingPop != null)
            {
                // Update existing pop size
                matchingPop.Size = incomingPop.Size;
            }
            else
            {
                // Add new pop
                result.Pops.Add(incomingPop);
            }
        }

        return result;
    }
}
