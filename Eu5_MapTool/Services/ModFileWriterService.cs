using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
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
        if(Directory.Exists(nameHexDir))
            _nameHexFileName = Directory.GetFiles(nameHexDir).FirstOrDefault();
        
        string locationPopInfoDir = Path.Combine(_modsDirectory, StaticConstucts.MAPDATAPATH);
        if(Directory.Exists(locationPopInfoDir))
            _locationPopInfoFileName = Directory.GetFiles(locationPopInfoDir).Where(x => x.Contains("pop")).FirstOrDefault();
    }

    public async Task WriteLocationMapAsync(List<(string, string)> loc_name_hex)
    {
        if (string.IsNullOrWhiteSpace(_modsDirectory))
            throw new InvalidOperationException("Write directory is not set.");
        if (string.IsNullOrWhiteSpace(_nameHexFileName))
            throw new InvalidOperationException("Name-Hex file path is not set.");

        var filePath = Path.Combine(_modsDirectory, StaticConstucts.LOCHEXTONAMEPATH, _nameHexFileName);

        var lines = new List<string>();
        if (File.Exists(filePath))
        {
            lines = (await File.ReadAllLinesAsync(filePath))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }

        var dict = lines
            .Select(l => l.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0].Trim(),
                parts => parts[1].Trim(),
                StringComparer.OrdinalIgnoreCase);

        // Apply updates or additions
        foreach (var (hex, name) in loc_name_hex)
        {
            dict[hex] = name;
        }

        // Preserve ordering of original lines where possible
        var updatedLines = new List<string>();

        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                string hex = parts[0].Trim();
                if (dict.TryGetValue(hex, out var name))
                {
                    updatedLines.Add($"{hex} = {name}");
                    dict.Remove(hex); // remove processed entries
                    continue;
                }
            }
            updatedLines.Add(line);
        }

        // Add any new entries at the end
        foreach (var kvp in dict)
        {
            updatedLines.Add($"{kvp.Key} = {kvp.Value}");
        }

        await File.WriteAllLinesAsync(filePath, updatedLines);
    }
    
    private bool IsHexString(string input)
    {
        return input.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }

    public async Task WriteLocationInfoAsync(Dictionary<string, ProvinceLocation> locationInfo)
    {
        if (string.IsNullOrWhiteSpace(_modsDirectory))
            throw new InvalidOperationException("Write directory is not set.");

        string filePath = Path.Combine(_modsDirectory + StaticConstucts.MAPDATAPATH, "location.txt");
        
        Dictionary<string, string> existingEntries = new();

        if (File.Exists(filePath))
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Expect format like "08300 = { ... }"
                int spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    string id = trimmed.Substring(0, spaceIndex);
                    existingEntries[id] = trimmed;
                }
            }
        }
        
        foreach (var kvp in locationInfo)
        {
            string id = kvp.Key;
            var info = kvp.Value;

            string newLine = $"{id} = {{ " +
                             $"topography = {info.Topography} " +
                             $"vegetation = {info.Vegetation} " +
                             $"climate = {info.Climate} " +
                             $"religion = {info.Religion} " +
                             $"culture = {info.Culture} " +
                             $"raw_material = {info.RawMaterial} }}";

            existingEntries[id] = newLine; 
        }

        // Sort by province id before writing (for consistency)
        var outputLines = existingEntries
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToList();

        await File.WriteAllLinesAsync(filePath, outputLines);
    }

    public async Task WriteProvincePopInfoAsync(Dictionary<string, ProvincePopInfo> popInfo)
    {
        //TODO: Write the pop info to the appropriate mod file
        if (string.IsNullOrWhiteSpace(_modsDirectory))
            throw new InvalidOperationException("Write directory is not set.");
        
        // use this when it's a single file
        string filePath = Path.Combine(_modsDirectory + StaticConstucts.MAPDATAPATH, _locationPopInfoFileName);

        Dictionary<string, string> existingEntries = new();

        // pop format =>
        // stockholm = {
        //             define_pop = { type = noble size = 0.00021 culture = swedish religion = lutheran }
        //             define_pop = { type = peasant size = 0.0031 culture = swedish religion = lutheran }
        //             ..... 
        //         }
        if (File.Exists(filePath))
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            string? currentKey = null;
            StringBuilder currentBlock = new();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                
                if (line.EndsWith("{"))
                {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        currentKey = line[..eqIndex].Trim();
                        currentBlock.Clear();
                    }
                }
                else if (line == "}")
                {
                    // close current block
                    if (!string.IsNullOrEmpty(currentKey))
                    {
                        existingEntries[currentKey] = currentBlock.ToString().TrimEnd();
                        currentKey = null;
                    }
                }
                else if (currentKey != null)
                {
                    // inside a block
                    currentBlock.AppendLine(line);
                }
            }
        }
        foreach (var kvp in popInfo)
        {
            string provinceName = kvp.Key;
            ProvincePopInfo info = kvp.Value;

            // Build text block for the province
            var sb = new StringBuilder();

            foreach (var pop in info.Pops)
            {
                sb.AppendLine($"    define_pop = {{ type = {pop.PopType} size = {pop.Size.ToString("0.#######", System.Globalization.CultureInfo.InvariantCulture)} culture = {pop.Culture} religion = {pop.Religion} }}");
            }

            // Replace or add
            existingEntries[provinceName] = sb.ToString().TrimEnd();
        }
        
        var output = new StringBuilder();

        foreach (var kvp in existingEntries.OrderBy(k => k.Key)) // optional sorting
        {
            output.AppendLine($"{kvp.Key} = {{");
            output.AppendLine(kvp.Value);
            output.AppendLine("}");
            output.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, output.ToString());
    }
}