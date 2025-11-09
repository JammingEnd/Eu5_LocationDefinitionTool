using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Parsing;

/// <summary>
/// Represents a parsed nested structure entry.
/// </summary>
public class NestedStructureEntry
{
    public string Key { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Parser for nested structure files with brace notation.
/// Example format:
/// key = { property1 = value1 property2 = value2 }
///
/// Used for location_templates.txt files.
/// </summary>
public class NestedStructureParser : IFileParser<Dictionary<string, Dictionary<string, string>>>,
                                      IFileWriter<Dictionary<string, Dictionary<string, string>>>
{
    private readonly bool _ignoreComments;
    private readonly bool _supportMultiLine;

    public NestedStructureParser(bool ignoreComments = true, bool supportMultiLine = true)
    {
        _ignoreComments = ignoreComments;
        _supportMultiLine = supportMultiLine;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> ParseFileAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        return ParseLines(lines);
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> ParseFilesAsync(IEnumerable<string> filePaths)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

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

    public Dictionary<string, Dictionary<string, string>> ParseLines(string[] lines)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || (_ignoreComments && line.StartsWith('#')))
                continue;

            // Try to parse as single-line entry
            var singleLine = ParseSingleLineEntry(line);
            if (singleLine != null)
            {
                result[singleLine.Key] = singleLine.Properties;
                continue;
            }

            // Try to parse as multi-line entry
            if (_supportMultiLine)
            {
                var multiLine = ParseMultiLineEntry(lines, ref i);
                if (multiLine != null)
                {
                    result[multiLine.Key] = multiLine.Properties;
                }
            }
        }

        return result;
    }

    private NestedStructureEntry? ParseSingleLineEntry(string line)
    {
        // Format: KEY = { prop1 = val1 prop2 = val2 }
        int equalIndex = line.IndexOf('=');
        if (equalIndex <= 0)
            return null;

        string key = line[..equalIndex].Trim();
        string content = line[(equalIndex + 1)..].Trim();

        // Check if content is wrapped in braces
        if (!content.StartsWith('{') || !content.EndsWith('}'))
            return null;

        // Extract content between braces
        string innerContent = content[1..^1].Trim();

        // Parse properties
        var properties = ParseProperties(innerContent);

        return new NestedStructureEntry
        {
            Key = key,
            Properties = properties
        };
    }

    private NestedStructureEntry? ParseMultiLineEntry(string[] lines, ref int currentIndex)
    {
        var line = lines[currentIndex].Trim();

        // Format: KEY = {
        int equalIndex = line.IndexOf('=');
        if (equalIndex <= 0)
            return null;

        string key = line[..equalIndex].Trim();
        string afterEqual = line[(equalIndex + 1)..].Trim();

        if (!afterEqual.StartsWith('{'))
            return null;

        // Collect lines until closing brace
        var contentBuilder = new StringBuilder();
        int braceDepth = 1;
        bool firstLine = true;

        // Handle inline content on first line
        string remainingFirstLine = afterEqual[1..].Trim();
        if (!string.IsNullOrEmpty(remainingFirstLine))
        {
            if (remainingFirstLine.EndsWith('}'))
            {
                contentBuilder.Append(remainingFirstLine[..^1].Trim());
                braceDepth = 0;
            }
            else
            {
                contentBuilder.Append(remainingFirstLine);
            }
            firstLine = false;
        }

        currentIndex++;

        while (currentIndex < lines.Length && braceDepth > 0)
        {
            var contentLine = lines[currentIndex].Trim();

            // Count braces
            foreach (char c in contentLine)
            {
                if (c == '{') braceDepth++;
                else if (c == '}') braceDepth--;
            }

            // Remove closing brace if we're done
            if (braceDepth == 0)
            {
                int lastBrace = contentLine.LastIndexOf('}');
                contentLine = contentLine[..lastBrace].Trim();
            }

            if (!string.IsNullOrEmpty(contentLine))
            {
                if (!firstLine)
                    contentBuilder.Append(' ');
                contentBuilder.Append(contentLine);
                firstLine = false;
            }

            currentIndex++;
        }

        if (braceDepth != 0)
            return null; // Malformed structure

        currentIndex--; // Back up one line

        var properties = ParseProperties(contentBuilder.ToString());

        return new NestedStructureEntry
        {
            Key = key,
            Properties = properties
        };
    }

    private Dictionary<string, string> ParseProperties(string content)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Split by space, handling = signs
        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "=")
                continue;

            // Check if next is equals
            if (i + 2 < parts.Length && parts[i + 1] == "=")
            {
                string propKey = parts[i];
                string propValue = parts[i + 2];
                properties[propKey] = propValue;
                i += 2; // Skip = and value
            }
            else if (parts[i].Contains('='))
            {
                // Handle key=value without spaces
                var keyValue = parts[i].Split('=', 2);
                if (keyValue.Length == 2)
                {
                    properties[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
        }

        return properties;
    }

    public async Task WriteFileAsync(string filePath, Dictionary<string, Dictionary<string, string>> data)
    {
        var lines = SerializeToLines(data);
        await File.WriteAllLinesAsync(filePath, lines);
    }

    public string[] SerializeToLines(Dictionary<string, Dictionary<string, string>> data)
    {
        var lines = new List<string>();

        foreach (var entry in data)
        {
            var propertiesStr = string.Join(" ", entry.Value.Select(p => $"{p.Key} = {p.Value}"));
            lines.Add($"{entry.Key} = {{ {propertiesStr} }}");
        }

        return lines.ToArray();
    }
}
