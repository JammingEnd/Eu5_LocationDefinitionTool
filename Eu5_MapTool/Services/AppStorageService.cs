using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;

namespace Eu5_MapTool.Services;

public class AppStorageService : IAppStorageInterface
{
    private string _baseGamePath;
    private string _moddedGamePath;

    private const string _LocationHexToNamefile = "";
    
    
    public void SetDirectories(string baseGamePath, string moddedGamePath)
    {
        _baseGamePath = baseGamePath;
        _moddedGamePath = moddedGamePath;
    }
    
    public async Task<Dictionary<string, ProvinceInfo>> LoadBaseGameAsync()
    {
        Dictionary<string, ProvinceInfo> provinceInfos = new();
        
        // reading hex to name mapping 
        string locationHexToNamePath = Path.Combine(_baseGamePath, StaticConstucts._LocationHexToNameDirectory);
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
        
        
        
        string locationFilePath = Path.Combine(_baseGamePath + StaticConstucts._ProvinceLocationFileName, "Location.txt");
        
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
        
        
        
        
        return provinceInfos;
    }
    public async Task<Dictionary<string, ProvinceInfo>> LoadModdedAsync()
    {
        Dictionary<string, ProvinceInfo> provinceInfos = new();
        
        // reading hex to name mapping 
        string locationHexToNamePath = Path.Combine(_moddedGamePath, StaticConstucts._LocationHexToNameDirectory); 
        var dirInfo = new DirectoryInfo(locationHexToNamePath);
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
                
                string name = line[..equalIndex].Trim();
                string hex = line[(equalIndex + 1)..].Trim();
                
                ProvinceInfo info = new ProvinceInfo(hex, name);
                
                provinceInfos[hex] = info;
            }   
            
        }
        
        
        
        string locationFilePath = Path.Combine(_moddedGamePath + StaticConstucts._ProvinceLocationFileName, "Location.txt");
        
        string popFilePath = Path.Combine(_moddedGamePath + StaticConstucts._ProvinceLocationFileName, "PopLocationInfo.txt");
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
        
        
        
        
        
        
        return provinceInfos;
    }

    public async Task<Bitmap> LoadMapImageAsync()
    {
        using var fs = File.OpenRead(Path.Combine(_moddedGamePath + StaticConstucts._ProvinceLocationFileName, "locations.png"));
        return new Bitmap(fs);
    }
    
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
        
        return result;
    }
        
}