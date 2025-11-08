using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services;

public class AppStorageService : IAppStorageInterface
{
    private string _baseGamePath = "";
    private string _moddedGamePath ="";
    public void SetDirectories(string baseGamePath, string moddedGamePath)
    {
        _baseGamePath = baseGamePath;
        _moddedGamePath = moddedGamePath;
    }
    
    public async Task<Dictionary<string, ProvinceInfo>> LoadBaseGameAsync()
    {
        Dictionary<string, ProvinceInfo> provinceInfos = new();
        
        // reading hex to name mapping 
        string locationHexToNamePath = Path.Combine(_baseGamePath, StaticConstucts.LOCHEXTONAMEPATH);
        var dirInfo = new DirectoryInfo(locationHexToNamePath);
        foreach (FileInfo locMapFiles in dirInfo.GetFiles("*.txt"))
        {
            var c_lines = await File.ReadAllLinesAsync(locMapFiles.DirectoryName);
            Queue<string> mapLines = new Queue<string>(
                c_lines.Where(line => !string.IsNullOrWhiteSpace(line))
            );
            // line format -> location_name = FFF000
            while (mapLines.Count > 0)
            {
                string line = mapLines.Dequeue();
                int equalIndex = line.IndexOf('=');
                if (equalIndex < 0) continue;
                
                string name = line[..equalIndex].Trim();
                string hex = line[(equalIndex + 1)..].Trim();
                
                ProvinceInfo info = new ProvinceInfo(hex, name);
                
                provinceInfos[hex] = info;
            }   
            
        }
        
        Console.WriteLine("Base province mapping loaded: " + provinceInfos.Count + " entries.");
        
        
        
        string locationFilePath = Path.Combine(_baseGamePath + StaticConstucts.MAPDATAPATH, "location_templates.txt");
        
        // read all lines from Location.txt and parsing it to ProvinceInfo objects
        var lines = await File.ReadAllLinesAsync(locationFilePath);
        Queue<string> locationLines = new Queue<string>(
            lines.Where(line => !string.IsNullOrWhiteSpace(line))
        );

        while (locationLines.Count > 0)
        {
            string line = locationLines.Dequeue();
            int idEnd = line.IndexOf('=');
            string id = line[..idEnd].Trim();
            
            string inner = line[(idEnd + 1)..].Trim('{', ' ', '}');
            
            string[] parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var dict = new Dictionary<string, string>();

            for (int i = 0; i < parts.Length - 2; i += 3)
            {
                string key = parts[i];
                string value = parts[i + 2]; // skip '='
                dict[key] = value;
            }

            ProvinceInfo info = provinceInfos[id];
            
              info.LocationInfo = new ProvinceLocation
                {
                    Topography = dict.GetValueOrDefault("topography"),
                    Vegetation = dict.GetValueOrDefault("vegetation"),
                    Climate = dict.GetValueOrDefault("climate"),
                    Religion = dict.GetValueOrDefault("religion"),
                    Culture = dict.GetValueOrDefault("culture"),
                    RawMaterial = dict.GetValueOrDefault("raw_material"),
                    NaturalHarborSuitability = dict.GetValueOrDefault("natural_harbor_suitability", "0.00")

                };
            
        }
        
        Console.WriteLine("base Locations loaded: " + provinceInfos.Count + " entries.");
        
        
        return provinceInfos;
    }
    public async Task<Dictionary<string, ProvinceInfo>> LoadModdedAsync()
    {
        Dictionary<string, ProvinceInfo> provinceInfos = new();
        
        // reading hex to name mapping 
        string locationHexToNamePath = Path.Combine(_moddedGamePath, StaticConstucts.LOCHEXTONAMEPATH); 
        var dirInfo = new DirectoryInfo(locationHexToNamePath);

        if (!dirInfo.Exists)
            throw new DirectoryNotFoundException("Modded location hex to name directory not found: " + locationHexToNamePath);
            
        
        foreach (FileInfo locMapFiles in dirInfo.GetFiles("*.txt"))
        {
            var c_lines = await File.ReadAllLinesAsync(locMapFiles.FullName);
            Queue<string> mapLines = new Queue<string>(
                c_lines.Where(line => !string.IsNullOrWhiteSpace(line))
            );
            
            // line format -> location_name = FFF000
            while (mapLines.Count > 0)
            {
                string line = mapLines.Dequeue().Trim();

                // Skip empty lines or comment/separator lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("####"))
                    continue;
                
                int commentIndex = line.IndexOf('#');
                if (commentIndex >= 0)
                    line = line[..commentIndex].Trim();
                
                int equalIndex = line.IndexOf('=');
                if (equalIndex < 0)
                    continue;
                
                string hex = line[..equalIndex].Trim();
                string name = line[(equalIndex + 1)..].Trim();

                // Skip if either side is empty
                if (string.IsNullOrEmpty(hex) || string.IsNullOrEmpty(name))
                    continue;

                // Create and store the province info
                ProvinceInfo info = new ProvinceInfo(hex, name);
                info.OldName = hex;
                provinceInfos[hex] = info;
                
                StaticConstucts.HEXTONAMEMAP[hex] = name;
            }

            
        }
        Console.WriteLine("Modded Locations mapping loaded: " + provinceInfos.Count + " entries.");
        
        
        string locationFilePath = Path.Combine(_moddedGamePath + StaticConstucts.MAPDATAPATH, "location_templates.txt");
        string popFilePath = Path.Combine(_moddedGamePath + StaticConstucts.POPINFOPATH);
        
        
        string popLocFilename = "";
        string popLocFull = "";
        if (Directory.Exists(popFilePath))
        { 
            popLocFilename = Directory.GetFiles(popFilePath).Where(x => x.Contains("pops")).FirstOrDefault();
            popLocFull = Path.Combine(popFilePath, popLocFilename);
            
        }
        var popInfos = await LoadProvincePopInfoAsync(popLocFull);
        // read all lines from Location.txt and parsing it to ProvinceInfo objects
        var lines = await File.ReadAllLinesAsync(locationFilePath);
        Queue<string> locationLines = new Queue<string>(
            lines.Where(line => !string.IsNullOrWhiteSpace(line))
        );
        //locationLines.Dequeue();


        while (locationLines.Count > 0)
        {
            string line = locationLines.Dequeue();
            int idEnd = line.IndexOf('=');
            string id = line[..idEnd].Trim();
            
            string inner = line[(idEnd + 1)..].Trim('{', ' ', '}');
            
            string[] parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var dict = new Dictionary<string, string>();

            for (int i = 0; i < parts.Length - 2; i += 3)
            {
                string key = parts[i];
                string value = parts[i + 2]; // skip '='
                dict[key] = value;
            }
            
            try
            {
                ProvinceInfo guh = provinceInfos[id];
                
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Modded Location id not found in mapping: " + id);
                continue;
            }
            ProvinceInfo info = provinceInfos[id];
            
            info.LocationInfo = new ProvinceLocation
            {
                Topography = dict.GetValueOrDefault("topography"),
                Vegetation = dict.GetValueOrDefault("vegetation"),
                Climate = dict.GetValueOrDefault("climate"),
                Religion = dict.GetValueOrDefault("religion"),
                Culture = dict.GetValueOrDefault("culture"),
                RawMaterial = dict.GetValueOrDefault("raw_material"),
                NaturalHarborSuitability = dict.GetValueOrDefault("natural_harbor_suitability")
            };
            if (popInfos.TryGetValue(id, out var popInfo))
            {
                info.PopInfo = popInfo;
            }
              
            
        }
        
        Console.WriteLine("Modded Locations loaded: " + provinceInfos.Count + " entries.");
        
        // its sucks that i have to do this  
        Dictionary<string, ProvinceInfo> finalProvinceInfos = new();
        
        foreach (var kvp in provinceInfos)
        {
            if (kvp.Key == "hoorn")
            {
                Console.WriteLine("googoogaaga");
            }
            finalProvinceInfos[kvp.Value.Id] = kvp.Value;
        }
        
        return finalProvinceInfos;
    }

    public async Task<Bitmap> LoadMapImageAsync()
    {
        await using var fs = File.OpenRead(Path.Combine(_moddedGamePath + StaticConstucts.MAPDATAPATH, "locations.png"));
        Console.WriteLine("Image loaded.");
        return new Bitmap(fs);
    }

    private async Task<HashSet<string>> LoadListAsync(string directoryPath, string subPath)
    {
        string fullPath = Path.Combine(directoryPath, StaticConstucts.COMMONPATH + subPath);
        var dirInfo = new DirectoryInfo(fullPath);

        HashSet<string> result = new HashSet<string>();
        if (!dirInfo.Exists)
            return result;

        var fileInfos = dirInfo.GetFiles("*.txt");
        if (fileInfos.Length == 0)
            throw new FileNotFoundException($"{subPath} files not found: " + fullPath);

        var regex = new Regex(@"^\s*(\w+)\s*=");

        foreach (var fileInfo in fileInfos)
        {
            var lines = await File.ReadAllLinesAsync(fileInfo.FullName);
            int braceDepth = 0;

            foreach (var line in lines)
            {
                if (braceDepth == 0)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                        result.Add(match.Groups[1].Value);
                }
                braceDepth += line.Count(c => c == '{') - line.Count(c => c == '}');
            }
        }

        return result;
    }


public Task<HashSet<string>> LoadTopographyListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "topography/");

public Task<HashSet<string>> LoadClimateListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "climates/");

public Task<HashSet<string>> LoadVegetationListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "vegetation/");

public Task<HashSet<string>> LoadReligionListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "religions/");

public Task<HashSet<string>> LoadCultureListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "cultures/");

public async Task<HashSet<string>> LoadRawMaterialListAsync(string directoryPath)
{
    string fullPath = Path.Combine(directoryPath, StaticConstucts.COMMONPATH + "goods/");
    var dirInfo = new DirectoryInfo(fullPath);

    HashSet<string> result = new HashSet<string>();
    if (!dirInfo.Exists)
        return result;

    var fileInfos = dirInfo.GetFiles("*.txt");
    if (fileInfos.Length == 0)
        throw new FileNotFoundException("No raw materials files found in: " + fullPath);

    var blockHeaderRegex = new Regex(@"^\s*(\w+)\s*=");

    foreach (var fileInfo in fileInfos)
    {
        var lines = await File.ReadAllLinesAsync(fileInfo.FullName);
        string? currentBlock = null;
        bool isRawMaterial = false;
        int braceDepth = 0;

        foreach (var line in lines)
        {
            var headerMatch = blockHeaderRegex.Match(line);
            if (headerMatch.Success && braceDepth == 0)
            {
                currentBlock = headerMatch.Groups[1].Value;
                isRawMaterial = false;
            }

            braceDepth += line.Count(c => c == '{') - line.Count(c => c == '}');

            if (line.Contains("category") && line.Contains("raw_material"))
                isRawMaterial = true;

            if (braceDepth == 0 && currentBlock != null)
            {
                if (isRawMaterial)
                    result.Add(currentBlock);
                currentBlock = null;
            }
        }
    }

    return result;
}

public Task<HashSet<string>> LoadPopTypeListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "pop_types/");

private async Task<Dictionary<string, ProvincePopInfo>> LoadProvincePopInfoAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Province pop file not found", filePath);
        
        var lines = await File.ReadAllLinesAsync(filePath);
        
        var result = new Dictionary<string, ProvincePopInfo>();
        string? currentProvince = null;
        var currentPops = new List<PopDef>();
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.Contains("locations"))
                continue;
        
            // province start: stockholm = {
            if (line.EndsWith("{") && line.Contains('='))
            {
                int eq = line.IndexOf('=');
                currentProvince = line[..eq].Trim();
                currentPops.Clear();
                continue;
            }
        
            // end of province block
            if (line == "}" && currentProvince != null)
            {
                result[currentProvince] = new ProvincePopInfo { Pops = new List<PopDef>(currentPops) };
                currentProvince = null;
                continue;
            }
            
            // inside block: define_pop = { type = noble size = 0.00021 culture = swedish religion = lutheran }
            if (currentProvince != null && line.StartsWith("define_pop"))
            {
                var pop = new PopDef();

                int start = line.IndexOf('{');
                int end = line.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    var inner = line.Substring(start + 1, end - start - 1).Trim();

                    // Split on spaces or tabs, remove empty entries
                    var parts = inner.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < parts.Length; i++)
                    {
                        switch (parts[i])
                        {
                            case "type":
                                if (i + 2 < parts.Length && parts[i + 1] == "=")
                                    pop.PopType = parts[i + 2];
                                break;
                            case "size":
                                if (i + 2 < parts.Length && parts[i + 1] == "=")
                                {
                                    if (float.TryParse(parts[i + 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s))
                                        pop.Size = (float)Math.Round(s, 5);
                                }
                                break;
                            case "culture":
                                if (i + 2 < parts.Length && parts[i + 1] == "=")
                                    pop.Culture = parts[i + 2];
                                break;
                            case "religion":
                                if (i + 2 < parts.Length && parts[i + 1] == "=")
                                    pop.Religion = parts[i + 2];
                                break;
                        }
                    }

                    currentPops.Add(pop);
                }
            }
        }
        Console.WriteLine("Modded Location pop info loaded: " + result.Count + " entries.");
        return result;
    }
    

        
}