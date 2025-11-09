using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eu5_MapTool.cache;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services.Parsing;

namespace Eu5_MapTool.Services;

/// <summary>
/// Service for loading game definition caches using parsers.
/// Replaces AppStorageService cache loading functionality.
/// </summary>
public class CacheLoaderService
{
    /// <summary>
    /// Load all game definition caches (religions, cultures, topography, etc.)
    /// from both base game and modded directories.
    /// </summary>
    public async Task<Cache> LoadCacheAsync(string baseGamePath, string moddedGamePath)
    {
        Console.WriteLine("Loading game definition caches using parsers...");

        // Create parsers
        var definitionParser = new GameDefinitionParser(ignoreComments: true);
        var rawMaterialParser = new GameDefinitionParser(ignoreComments: true, categoryFilter: "raw_material");

        // Load all definition types
        var baseCultures = await LoadDefinitionsAsync(definitionParser, baseGamePath, "cultures/");
        var modCultures = await LoadDefinitionsAsync(definitionParser, moddedGamePath, "cultures/");

        var baseReligions = await LoadDefinitionsAsync(definitionParser, baseGamePath, "religions/");
        var modReligions = await LoadDefinitionsAsync(definitionParser, moddedGamePath, "religions/");

        var baseTopography = await LoadDefinitionsAsync(definitionParser, baseGamePath, "topography/");
        var modTopography = await LoadDefinitionsAsync(definitionParser, moddedGamePath, "topography/");

        var baseVegetation = await LoadDefinitionsAsync(definitionParser, baseGamePath, "vegetation/");
        var modVegetation = await LoadDefinitionsAsync(definitionParser, moddedGamePath, "vegetation/");

        var baseClimates = await LoadDefinitionsAsync(definitionParser, baseGamePath, "climates/");
        var modClimates = await LoadDefinitionsAsync(definitionParser, moddedGamePath, "climates/");

        var baseRawMaterials = await LoadDefinitionsAsync(rawMaterialParser, baseGamePath, "goods/");
        var modRawMaterials = await LoadDefinitionsAsync(rawMaterialParser, moddedGamePath, "goods/");

        var basePopTypes = await LoadDefinitionsAsync(definitionParser, baseGamePath, "pop_types/");
        var modPopTypes = await LoadDefinitionsAsync(definitionParser, moddedGamePath, "pop_types/");

        // Construct cache objects
        var cache = new Cache(
            new ReligionC(baseReligions, modReligions),
            new CulturesC(baseCultures, modCultures),
            new TopographyC(baseTopography, modTopography),
            new VegetationC(baseVegetation, modVegetation),
            new ClimateC(baseClimates, modClimates),
            new RawMaterialsC(baseRawMaterials, modRawMaterials),
            new PopTypesC(basePopTypes, modPopTypes)
        );

        Console.WriteLine("✓ Game definition caches loaded successfully");
        Console.WriteLine($"  - Religions: {cache.Religions.GetCombined().Count}");
        Console.WriteLine($"  - Cultures: {cache.Cultures.GetCombined().Count}");
        Console.WriteLine($"  - Topography: {cache.Topographies.GetCombined().Count}");
        Console.WriteLine($"  - Vegetation: {cache.Vegetations.GetCombined().Count}");
        Console.WriteLine($"  - Climates: {cache.Climates.GetCombined().Count}");
        Console.WriteLine($"  - Raw Materials: {cache.RawMaterials.GetCombined().Count}");
        Console.WriteLine($"  - Pop Types: {cache.PopTypes.GetCombined().Count}");

        return cache;
    }

    /// <summary>
    /// Load definitions from a specific directory and subdirectory.
    /// </summary>
    private async Task<HashSet<string>> LoadDefinitionsAsync(
        GameDefinitionParser parser,
        string basePath,
        string subPath)
    {
        string fullPath = Path.Combine(basePath, StaticConstucts.COMMONPATH, subPath);
        var dirInfo = new DirectoryInfo(fullPath);

        if (!dirInfo.Exists)
        {
            Console.WriteLine($"Warning: Definition directory not found: {fullPath}");
            return new HashSet<string>();
        }

        var files = dirInfo.GetFiles("*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine($"Warning: No definition files found in: {fullPath}");
            return new HashSet<string>();
        }

        var filePaths = files.Select(f => f.FullName);
        return await parser.ParseFilesAsync(filePaths);
    }
}
