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
        
        
        
        string locationFilePath = Path.Combine(_baseGamePath + StaticConstucts.MAPDATAPATH, "Location.txt");
        
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
                    Topography = dict["topography"],
                    Vegetation = dict["vegetation"],
                    Climate = dict["climate"],
                    Religion = dict["religion"],
                    Culture = dict["culture"],
                    RawMaterial = dict["raw_material"]

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
                string line = mapLines.Dequeue();
                int equalIndex = line.IndexOf('=');
                if (equalIndex < 0) continue;
                
                string hex = line[..equalIndex].Trim();
                string name = line[(equalIndex + 1)..].Trim();
                
                ProvinceInfo info = new ProvinceInfo(hex, name);
                
                provinceInfos[hex] = info;
            }   
            
        }
        Console.WriteLine("Modded Locations mapping loaded: " + provinceInfos.Count + " entries.");
        
        
        string locationFilePath = Path.Combine(_moddedGamePath + StaticConstucts.MAPDATAPATH, "location.txt");
        string locationPath = Path.Combine(_moddedGamePath, StaticConstucts.MAPDATAPATH);
        
        string popLocFilename = "";
        if(Directory.Exists(locationPath))
            popLocFilename = Directory.GetFiles(locationPath).Where(x => x.Contains("pop")).FirstOrDefault();
        string popFilePath = Path.Combine(_moddedGamePath + StaticConstucts.MAPDATAPATH, popLocFilename);
        var popInfos = await LoadProvincePopInfoAsync(popFilePath);
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
                Topography = dict["topography"],
                Vegetation = dict["vegetation"],
                Climate = dict["climate"],
                Religion = dict["religion"],
                Culture = dict["culture"],
                RawMaterial = dict["raw_material"]
            };
            if (popInfos.TryGetValue(id, out var popInfo))
            {
                info.PopInfo = popInfo;
            }
              
            
        }
        
        Console.WriteLine("Modded Locations loaded: " + provinceInfos.Count + " entries.");
        
        return provinceInfos;
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

    var fileInfo = dirInfo.GetFiles("*.txt").FirstOrDefault();
    if (fileInfo == null) throw new FileNotFoundException($"{subPath} file not found: " + fullPath);

    var lines = await File.ReadAllLinesAsync(fileInfo.FullName);
    int braceDepth = 0;
    var regex = new Regex(@"^\s*(\w+)\s*=");

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

    var fileInfo = dirInfo.GetFiles("*.txt").FirstOrDefault();
    if (fileInfo == null) throw new FileNotFoundException("Raw materials file not found: " + fullPath);

    var lines = await File.ReadAllLinesAsync(fileInfo.FullName);
    string? currentBlock = null;
    bool isRawMaterial = false;
    int braceDepth = 0;
    var blockHeaderRegex = new Regex(@"^\s*(\w+)\s*=");

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

    return result;
}

public Task<HashSet<string>> LoadPopTypeListAsync(string directoryPath) =>
    LoadListAsync(directoryPath, "pop_types/");

    public async Task<Dictionary<string, ProvincePopInfo>> LoadProvincePopInfoAsync(string filePath)
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
            if (string.IsNullOrWhiteSpace(line))
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
        
                // get content between { and }
                int start = line.IndexOf('{');
                int end = line.LastIndexOf('}');
                if (start >= 0 && end > start)
                {
                    var inner = line.Substring(start + 1, end - start - 1).Trim();
                    var parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
                    // parts are like [type, =, noble, size, =, 0.00021, ...]
                    for (int i = 0; i < parts.Length - 2; i++)
                    {
                        if (parts[i] == "=") continue;
                        switch (parts[i])
                        {
                            case "type":
                                pop.PopType = parts[2]; // value after =
                                break;
                            case "size":
                                if (float.TryParse(parts[5], System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out float s))
                                    pop.Size = (float)Math.Round(s, 5);
                                break;
                            case "culture":
                                pop.Culture = parts[8];
                                break;
                            case "religion":
                                pop.Religion = parts[11];
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