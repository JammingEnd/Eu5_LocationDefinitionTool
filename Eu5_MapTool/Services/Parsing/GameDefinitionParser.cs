using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Eu5_MapTool.Services.Parsing;

/// <summary>
/// Parser for game definition files (religions, cultures, topography, etc.).
/// These files typically contain key-value pairs within nested structures.
/// The parser extracts definition names (keys) from the file.
/// </summary>
public class GameDefinitionParser : IFileParser<HashSet<string>>
{
    private readonly bool _ignoreComments;
    private readonly string? _categoryFilter; // For filtering by category (e.g., "raw_material")

    public GameDefinitionParser(bool ignoreComments = true, string? categoryFilter = null)
    {
        _ignoreComments = ignoreComments;
        _categoryFilter = categoryFilter;
    }

    public async Task<HashSet<string>> ParseFileAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        return ParseLines(lines);
    }

    public async Task<HashSet<string>> ParseFilesAsync(IEnumerable<string> filePaths)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in filePaths)
        {
            var fileData = await ParseFileAsync(filePath);
            foreach (var item in fileData)
            {
                result.Add(item);
            }
        }

        return result;
    }

    public HashSet<string> ParseLines(string[] lines)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int braceDepth = 0;
        string? currentKey = null;
        bool currentItemMatchesFilter = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || (_ignoreComments && trimmedLine.StartsWith('#')))
                continue;

            // Track brace depth
            int openBraces = trimmedLine.Count(c => c == '{');
            int closeBraces = trimmedLine.Count(c => c == '}');

            // Look for definition keys (format: "key = {")
            if (trimmedLine.Contains('=') && trimmedLine.Contains('{'))
            {
                int equalIndex = trimmedLine.IndexOf('=');
                string potentialKey = trimmedLine[..equalIndex].Trim();

                // This is a new definition
                if (braceDepth == 0)
                {
                    currentKey = potentialKey;
                    currentItemMatchesFilter = string.IsNullOrEmpty(_categoryFilter); // Default to true if no filter
                }
            }

            // Check for category filter
            if (!string.IsNullOrEmpty(_categoryFilter) && currentKey != null && braceDepth > 0)
            {
                if (trimmedLine.Contains("category") && trimmedLine.Contains('='))
                {
                    int equalIndex = trimmedLine.IndexOf('=');
                    string value = trimmedLine[(equalIndex + 1)..].Trim();

                    // Remove quotes and trailing characters
                    value = value.Trim('"', ' ', ',', '}');

                    if (value.Equals(_categoryFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        currentItemMatchesFilter = true;
                    }
                }
            }

            braceDepth += openBraces;
            braceDepth -= closeBraces;

            // When we close back to depth 0, add the key if it matches
            if (braceDepth == 0 && currentKey != null)
            {
                if (currentItemMatchesFilter)
                {
                    result.Add(currentKey);
                }
                currentKey = null;
                currentItemMatchesFilter = false;
            }
        }

        return result;
    }
}
