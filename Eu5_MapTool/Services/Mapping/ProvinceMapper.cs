using System;
using System.Collections.Generic;
using System.Linq;
using Eu5_MapTool.Models;
using Eu5_MapTool.logic;
using Eu5_MapTool.Services.Parsing;

namespace Eu5_MapTool.Services.Mapping;

/// <summary>
/// Orchestrates mapping for complete ProvinceInfo entities.
/// Combines LocationMapper and PopInfoMapper to handle all province data.
/// </summary>
public class ProvinceMapper
{
    private readonly LocationMapper _locationMapper;
    private readonly PopInfoMapper _popInfoMapper;

    public ProvinceMapper()
    {
        _locationMapper = new LocationMapper();
        _popInfoMapper = new PopInfoMapper();
    }

    /// <summary>
    /// Create a ProvinceInfo from hex-to-name mapping and location data.
    /// </summary>
    public ProvinceInfo MapToEntity(string hexId, string name, Dictionary<string, string>? locationData, LocationPopData? popData)
    {
        var province = new ProvinceInfo(name, hexId)
        {
            OldName = name
        };

        // Map location info if available
        if (locationData != null && locationData.Count > 0)
        {
            province.LocationInfo = _locationMapper.MapToEntity(locationData);
        }
        else
        {
            // Default empty location
            province.LocationInfo = new ProvinceLocation(
                string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, "0.00");
        }

        // Map pop info if available
        if (popData != null)
        {
            province.PopInfo = _popInfoMapper.MapToEntity(popData);
        }
        else
        {
            province.PopInfo = new ProvincePopInfo();
        }

        return province;
    }

    /// <summary>
    /// Create a dictionary of ProvinceInfo from all file data sources.
    /// </summary>
    public Dictionary<string, ProvinceInfo> MapToEntityDictionary(
        Dictionary<string, string> hexToNameMap,
        Dictionary<string, Dictionary<string, string>> locationData,
        Dictionary<string, LocationPopData> popData)
    {
        var result = new Dictionary<string, ProvinceInfo>(StringComparer.OrdinalIgnoreCase);

        // Primary iteration over hex-to-name mapping (this defines all provinces)
        foreach (var kvp in hexToNameMap)
        {
            string hexId = kvp.Value;  // Value is the hex ID
            string name = kvp.Key;     // Key is the name

            locationData.TryGetValue(hexId, out var locData);
            popData.TryGetValue(name, out var popDataEntry);

            var province = MapToEntity(hexId, name, locData, popDataEntry);
            result[hexId] = province;
        }

        // Also check for locations that might not have names yet
        foreach (var kvp in locationData)
        {
            string hexId = kvp.Key;
            if (!result.ContainsKey(hexId))
            {
                // This location has data but no name mapping
                var province = MapToEntity(hexId, hexId, kvp.Value, null);
                result[hexId] = province;
            }
        }

        return result;
    }

    /// <summary>
    /// Extract hex-to-name mapping from provinces.
    /// </summary>
    public Dictionary<string, string> MapToHexNameDictionary(Dictionary<string, ProvinceInfo> provinces)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var province in provinces.Values)
        {
            result[province.Name] = province.Id;
        }

        return result;
    }

    /// <summary>
    /// Extract location data from provinces.
    /// Uses province NAME as key (not hex ID) since location_templates file uses names.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> MapToLocationDataDictionary(Dictionary<string, ProvinceInfo> provinces)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var province in provinces.Values)
        {
            // Only include if location has meaningful data
            if (!string.IsNullOrEmpty(province.LocationInfo.Topography) ||
                !string.IsNullOrEmpty(province.LocationInfo.Vegetation) ||
                !string.IsNullOrEmpty(province.LocationInfo.Climate))
            {
                // Use province NAME as key (location_templates file uses names, not hex IDs)
                result[province.Name] = _locationMapper.MapToFileData(province.LocationInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Extract pop data from provinces.
    /// </summary>
    public Dictionary<string, LocationPopData> MapToPopDataDictionary(Dictionary<string, ProvinceInfo> provinces)
    {
        var result = new Dictionary<string, LocationPopData>(StringComparer.OrdinalIgnoreCase);

        foreach (var province in provinces.Values)
        {
            // Only include if province has pops
            if (province.PopInfo?.Pops != null && province.PopInfo.Pops.Count > 0)
            {
                result[province.Name] = _popInfoMapper.MapToFileData(province.Name, province.PopInfo);
            }
        }

        return result;
    }
}
