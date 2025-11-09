using System;
using System.Collections.Generic;
using System.Linq;
using Eu5_MapTool.logic;

namespace Eu5_MapTool.Services.Mapping;

/// <summary>
/// Maps between ProvinceLocation domain model and file data structures.
/// </summary>
public class LocationMapper : IEntityMapper<ProvinceLocation, Dictionary<string, string>>
{
    public ProvinceLocation MapToEntity(Dictionary<string, string> fileData)
    {
        return new ProvinceLocation(
            topography: fileData.GetValueOrDefault("topography", string.Empty),
            vegetation: fileData.GetValueOrDefault("vegetation", string.Empty),
            climate: fileData.GetValueOrDefault("climate", string.Empty),
            religion: fileData.GetValueOrDefault("religion", string.Empty),
            culture: fileData.GetValueOrDefault("culture", string.Empty),
            rawMaterial: fileData.GetValueOrDefault("raw_material", string.Empty),
            naturalHarborSuitability: fileData.GetValueOrDefault("natural_harbor_suitability", "0.00")
        );
    }

    public IEnumerable<ProvinceLocation> MapToEntities(IEnumerable<Dictionary<string, string>> fileDataCollection)
    {
        return fileDataCollection.Select(MapToEntity);
    }

    public Dictionary<string, string> MapToFileData(ProvinceLocation entity)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["topography"] = entity.Topography,
            ["vegetation"] = entity.Vegetation,
            ["climate"] = entity.Climate,
            ["religion"] = entity.Religion,
            ["culture"] = entity.Culture,
            ["raw_material"] = entity.RawMaterial,
            ["natural_harbor_suitability"] = entity.NaturalHarborSuitability
        };
    }

    public IEnumerable<Dictionary<string, string>> MapToFileDataCollection(IEnumerable<ProvinceLocation> entities)
    {
        return entities.Select(MapToFileData);
    }
}
