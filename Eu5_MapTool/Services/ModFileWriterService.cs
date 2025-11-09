using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Input;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services;

public class ModFileWriterService : IModFileWriter
{
    private string _modsDirectory;
    private string _nameHexFileName = "";
    private string _locationPopInfoFileName = "";
    

    public void SetWriteDirectory(string modsDirectory)
    {
        _modsDirectory = modsDirectory;
        
        string nameHexDir = Path.Combine(_modsDirectory, StaticConstucts.LOCHEXTONAMEPATH);
        if (Directory.Exists(nameHexDir))
            _nameHexFileName = Directory.GetFiles(nameHexDir).FirstOrDefault();
        
        string locationPopInfoDir = Path.Combine(_modsDirectory, StaticConstucts.POPINFOPATH);
        if (Directory.Exists(locationPopInfoDir))
            _locationPopInfoFileName = Directory.GetFiles(locationPopInfoDir)
                .FirstOrDefault(x => x.Contains("pops"));
    }

    // --------------------------------------------------------------------
    // LOCATION NAME MAP  (hex ↔ name)
    // --------------------------------------------------------------------
    public async Task WriteLocationMapAsync(List<(string, string)> loc_name_hex)
    {
        if (string.IsNullOrWhiteSpace(_modsDirectory))
            throw new InvalidOperationException("Write directory is not set.");
        if (string.IsNullOrWhiteSpace(_nameHexFileName))
            throw new InvalidOperationException("Name-Hex file path is not set.");

        var filePath = Path.Combine(_modsDirectory, StaticConstucts.LOCHEXTONAMEPATH, _nameHexFileName);
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(filePath))
        {
            var lines = await File.ReadAllLinesAsync(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains('=')) continue;
                var parts = line.Split('=', 2);
                var name = parts[0].Trim();
                var hex = parts[1].Trim();
                dict[hex] = name; 
            }
        }

        // Overwrite or add
        foreach (var (hex, name) in loc_name_hex)
            dict[hex] = name;
        
        var updatedLines = dict
            .Select(kvp => $"{kvp.Value} = {kvp.Key}")
            .ToList();

        await File.WriteAllLinesAsync(filePath, updatedLines);
    }

    public async Task WriteLocationInfoAsync(Dictionary<string, ProvinceInfo> locationInfos)
{
    if (string.IsNullOrWhiteSpace(_modsDirectory))
        throw new InvalidOperationException("Write directory is not set.");

    string filePath = Path.Combine(_modsDirectory, StaticConstucts.MAPDATAPATH, "location_templates.txt");


    // format is: ___YMJrkTYLkr = { topography = flatland vegetation = jungle climate = tropical religion = bon culture = dakelh_culture raw_material = horses natural_harbor_suitability =  }
    //
    if (File.Exists(filePath))
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        var outputLinesList = new List<string>();
        var processedLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            string currentLine = line;
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
            {
                outputLinesList.Add(currentLine);
                continue;
            };

            string existingName = line.Split('=')[0].Trim();

            // Find matching entry by checking both current name and OldName
            ProvinceInfo matchingInfo = null;
            foreach (var kvp in locationInfos)
            {
                var info = kvp.Value;
                string oldName = info.OldName ?? info.Name;
                if (existingName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                {
                    matchingInfo = info;
                    break;
                }
            }

            if (matchingInfo != null)
            {
                // Update the entry with new name and values
                currentLine = ReplaceName(line, matchingInfo.Name);
                currentLine = ReplaceValue(currentLine, "topography", matchingInfo.LocationInfo.Topography);
                currentLine = ReplaceValue(currentLine, "climate", matchingInfo.LocationInfo.Climate);
                currentLine = ReplaceValue(currentLine, "vegetation", matchingInfo.LocationInfo.Vegetation);
                currentLine = ReplaceValue(currentLine, "religion", matchingInfo.LocationInfo.Religion);
                currentLine = ReplaceValue(currentLine, "culture", matchingInfo.LocationInfo.Culture);
                currentLine = ReplaceValue(currentLine, "raw_material", matchingInfo.LocationInfo.RawMaterial);
                currentLine = ReplaceValue(currentLine, "natural_harbor_suitability", matchingInfo.LocationInfo.NaturalHarborSuitability);
                outputLinesList.Add(currentLine);
                processedLocations.Add(matchingInfo.Name);
                continue;
            }

            outputLinesList.Add(currentLine);
        }

        // Add any new locations that weren't in the file
        foreach (var location in locationInfos)
        {
            if (processedLocations.Contains(location.Value.Name))
                continue;

            var info = location.Value;
            string newline = $"{location.Value.Name} = " + "{ " +
                             $"topography = {info.LocationInfo.Topography} " +
                             $"vegetation = {info.LocationInfo.Vegetation} " +
                             $"climate = {info.LocationInfo.Climate} " +
                             $"religion = {info.LocationInfo.Religion} " +
                             $"culture = {info.LocationInfo.Culture} " +
                             $"raw_material = {info.LocationInfo.RawMaterial} " +
                             $"natural_harbor_suitability = {info.LocationInfo.NaturalHarborSuitability} " +
                             "}";
            outputLinesList.Add(newline);
        }

        await File.WriteAllLinesAsync(filePath, outputLinesList);
    }
}
    
public async Task WriteProvincePopInfoAsync(Dictionary<string, ProvinceInfo> locationPopInfos)
{
    if (string.IsNullOrWhiteSpace(_modsDirectory))
        throw new InvalidOperationException("Write directory is not set.");

    string filePath = Path.Combine(_modsDirectory, StaticConstucts.POPINFOPATH, _locationPopInfoFileName);

    // Read existing file
    var fileLines = File.Exists(filePath)
        ? (await File.ReadAllLinesAsync(filePath)).ToList()
        : new List<string>();

    // Parse existing locations and their pop definitions
    var existingLocations = ParseLocationBlocks(fileLines, out bool hasWrapper);

    // Process each province: update existing or prepare new ones
    var processedOldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var kvp in locationPopInfos)
    {
        var province = kvp.Value;
        string oldName = province.OldName ?? province.Name;
        string newName = province.Name;

        // Check if we need to rename or update an existing location
        if (existingLocations.TryGetValue(oldName, out var locationBlock))
        {
            // Update the name if it changed
            locationBlock.Name = newName;

            // Parse existing pops
            var existingPops = new List<ParsedPop>();
            foreach (var popLine in locationBlock.PopLines)
            {
                var parsed = ParsePopLine(popLine);
                if (parsed != null)
                {
                    existingPops.Add(parsed);
                }
            }

            // Merge/update with incoming pops
            if (province.PopInfo?.Pops != null)
            {
                foreach (var incomingPop in province.PopInfo.Pops)
                {
                    // Check if there's a matching existing pop (same type, culture, religion)
                    var matchingPop = existingPops.FirstOrDefault(p =>
                        p.Matches(incomingPop.PopType, incomingPop.Culture, incomingPop.Religion));

                    if (matchingPop != null)
                    {
                        // Update the size
                        matchingPop.Size = incomingPop.Size;
                    }
                    else
                    {
                        // Add as new pop
                        existingPops.Add(new ParsedPop
                        {
                            PopType = incomingPop.PopType,
                            Size = incomingPop.Size,
                            Culture = incomingPop.Culture,
                            Religion = incomingPop.Religion
                        });
                    }
                }
            }

            // Convert back to pop lines
            locationBlock.PopLines = existingPops.Select(p => p.ToPopLine()).ToList();

            processedOldNames.Add(oldName);

            // If name changed, update the dictionary key
            if (oldName != newName)
            {
                existingLocations.Remove(oldName);
                existingLocations[newName] = locationBlock;
            }
        }
        else
        {
            // New location - create a new block
            var newBlock = new LocationBlock { Name = newName };
            if (province.PopInfo?.Pops != null)
            {
                var newPops = new List<ParsedPop>();
                foreach (var pop in province.PopInfo.Pops)
                {
                    newPops.Add(new ParsedPop
                    {
                        PopType = pop.PopType,
                        Size = pop.Size,
                        Culture = pop.Culture,
                        Religion = pop.Religion
                    });
                }
                newBlock.PopLines = newPops.Select(p => p.ToPopLine()).ToList();
            }
            existingLocations[newName] = newBlock;
        }
    }

    // Generate output
    var output = new List<string>();

    // Always add wrapper
    output.Add("locations = {");

    // Write all location blocks
    foreach (var locationBlock in existingLocations.Values)
    {
        output.Add($"    {locationBlock.Name} = {{");
        output.AddRange(locationBlock.PopLines);
        output.Add("    }");
    }

    // Close wrapper
    output.Add("}");

    await File.WriteAllLinesAsync(filePath, output);
}

private Dictionary<string, LocationBlock> ParseLocationBlocks(List<string> fileLines, out bool hasWrapper)
{
    var locations = new Dictionary<string, LocationBlock>(StringComparer.OrdinalIgnoreCase);
    LocationBlock currentBlock = null;
    hasWrapper = false;

    foreach (var line in fileLines)
    {
        string trimmed = line.Trim();

        // Check for wrapper
        if (trimmed.StartsWith("locations") && trimmed.Contains("=") && trimmed.Contains("{"))
        {
            hasWrapper = true;
            continue;
        }

        // Check for closing brace of wrapper (standalone})
        if (trimmed == "}" && currentBlock == null)
        {
            continue;
        }

        // Start of a location block (e.g., "___YqDJWccpNQ = {" or "stockholm = {")
        var locationMatch = Regex.Match(trimmed, @"^([A-Za-z0-9_]+)\s*=\s*\{$");
        if (locationMatch.Success)
        {
            string locationName = locationMatch.Groups[1].Value;
            currentBlock = new LocationBlock { Name = locationName };
            locations[locationName] = currentBlock;
            continue;
        }

        // End of a location block
        if (trimmed == "}" && currentBlock != null)
        {
            currentBlock = null;
            continue;
        }

        // Pop definition line inside a location block
        if (currentBlock != null && trimmed.StartsWith("define_pop"))
        {
            currentBlock.PopLines.Add(line); // Keep original indentation
        }
    }

    return locations;
}

private class LocationBlock
{
    public string Name { get; set; }
    public List<string> PopLines { get; set; } = new List<string>();
}

private class ParsedPop
{
    public string PopType { get; set; }
    public float Size { get; set; }
    public string Culture { get; set; }
    public string Religion { get; set; }

    public string ToPopLine()
    {
        return $"        define_pop = {{ type = {PopType} size = {Size} culture = {Culture} religion = {Religion} }}";
    }

    public bool Matches(string popType, string culture, string religion)
    {
        return string.Equals(PopType, popType, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Culture, culture, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Religion, religion, StringComparison.OrdinalIgnoreCase);
    }
}

private ParsedPop ParsePopLine(string line)
{
    // Parse line like: "        define_pop = { type = clergy size = 0.002 culture = wetsuweten_culture religion = anglican }"
    var match = Regex.Match(line, @"type\s*=\s*(\S+)\s+size\s*=\s*(\S+)\s+culture\s*=\s*(\S+)\s+religion\s*=\s*(\S+)");
    if (match.Success)
    {
        return new ParsedPop
        {
            PopType = match.Groups[1].Value,
            Size = float.Parse(match.Groups[2].Value),
            Culture = match.Groups[3].Value,
            Religion = match.Groups[4].Value
        };
    }
    return null;
}




    private string ReplaceValue(string input, string key, string value)
    {
        string pattern = $@"({key}\s*=\s*)(\S+)";
        return Regex.Replace(input, pattern, $"${{1}}{value}");

    }

    private string ReplaceName(string input, string value)
    {
        int index = input.IndexOf("=");
        if (index == -1) return input;
        
        string result = value.Trim() + " " + input.Substring(index);
        return result; 
    }


    private bool IsHexString(string input) =>
        input.All(c => "0123456789ABCDEFabcdef".Contains(c));

    
}
