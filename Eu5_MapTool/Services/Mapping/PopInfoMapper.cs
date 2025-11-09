using System;
using System.Collections.Generic;
using System.Linq;
using Eu5_MapTool.logic;
using Eu5_MapTool.Services.Parsing;

namespace Eu5_MapTool.Services.Mapping;

/// <summary>
/// Maps between ProvincePopInfo/PopDef domain models and PopDefinition file data.
/// </summary>
public class PopInfoMapper
{
    /// <summary>
    /// Map a LocationPopData to ProvincePopInfo.
    /// </summary>
    public ProvincePopInfo MapToEntity(LocationPopData fileData)
    {
        var popInfo = new ProvincePopInfo();

        foreach (var popDef in fileData.Pops)
        {
            popInfo.Pops.Add(new PopDef
            {
                PopType = popDef.PopType,
                Size = popDef.Size,
                Culture = popDef.Culture,
                Religion = popDef.Religion
            });
        }

        return popInfo;
    }

    /// <summary>
    /// Map ProvincePopInfo to LocationPopData.
    /// </summary>
    public LocationPopData MapToFileData(string locationName, ProvincePopInfo entity)
    {
        var fileData = new LocationPopData
        {
            LocationName = locationName,
            Pops = new List<PopDefinition>()
        };

        foreach (var pop in entity.Pops)
        {
            fileData.Pops.Add(new PopDefinition
            {
                PopType = pop.PopType,
                Size = pop.Size,
                Culture = pop.Culture,
                Religion = pop.Religion
            });
        }

        return fileData;
    }

    /// <summary>
    /// Map a dictionary of location names to ProvincePopInfo.
    /// </summary>
    public Dictionary<string, ProvincePopInfo> MapToEntityDictionary(Dictionary<string, LocationPopData> fileData)
    {
        var result = new Dictionary<string, ProvincePopInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in fileData)
        {
            result[kvp.Key] = MapToEntity(kvp.Value);
        }

        return result;
    }

    /// <summary>
    /// Map a dictionary of ProvincePopInfo to LocationPopData.
    /// </summary>
    public Dictionary<string, LocationPopData> MapToFileDataDictionary(Dictionary<string, ProvincePopInfo> entities)
    {
        var result = new Dictionary<string, LocationPopData>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in entities)
        {
            result[kvp.Key] = MapToFileData(kvp.Key, kvp.Value);
        }

        return result;
    }
}
