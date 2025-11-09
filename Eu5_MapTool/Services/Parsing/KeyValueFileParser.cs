using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Parsing;

/// <summary>
/// Parser for simple key = value file formats.
/// Used for named_locations files (e.g., "stockholm = ABABAB").
/// </summary>
public partial class KeyValueFileParser : IFileParser<Dictionary<string, string>>, IFileWriter<Dictionary<string, string>>
{
    private readonly bool _ignoreComments;
    private readonly bool _trimWhitespace;

    public KeyValueFileParser(bool ignoreComments = true, bool trimWhitespace = true)
    {
        _ignoreComments = ignoreComments;
        _trimWhitespace = trimWhitespace;
    }

    public async Task<Dictionary<string, string>> ParseFileAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        return ParseLines(lines);
    }

    public async Task<Dictionary<string, string>> ParseFilesAsync(IEnumerable<string> filePaths)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

    public Dictionary<string, string> ParseLines(string[] lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmedLine = _trimWhitespace ? line.Trim() : line;

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // Skip comments
            if (_ignoreComments && trimmedLine.StartsWith('#'))
                continue;

            // Find the equals sign
            int equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex <= 0)
                continue;

            // Extract key and value
            string key = trimmedLine[..equalIndex].Trim();
            string value = trimmedLine[(equalIndex + 1)..].Trim();

            // Store in dictionary (last value wins if duplicate)
            result[key] = value;
        }

        return result;
    }

    public async Task WriteFileAsync(string filePath, Dictionary<string, string> data)
    {
        var lines = SerializeToLines(data);
        await File.WriteAllLinesAsync(filePath, lines);
    }

    public string[] SerializeToLines(Dictionary<string, string> data)
    {
        return data.Select(kvp => $"{kvp.Key} = {kvp.Value}").ToArray();
    }
}
