using System.Collections.Generic;

namespace Eu5_MapTool.cache;
public class Cache
{
    public readonly ReligionC Religions = new ReligionC();
    public readonly CulturesC Cultures = new CulturesC();
    public readonly TopographyC Topographies = new TopographyC();
    public readonly VegetationC Vegetations = new VegetationC();
    public readonly ClimateC Climates = new ClimateC();
    public readonly RawMaterialsC RawMaterials = new RawMaterialsC();
    public readonly PopTypesC PopTypes = new PopTypesC();
    
    public Cache()
    {
        //TODO: use the StorageService to load cache from disk
        
        // temp data
        Religions.ReligionsBaseGame.UnionWith(new[] { "catholic", "protestant", "orthodox" });
        Cultures.CulturesBaseGame.UnionWith(new[] { "CoolCulture", "BasedCulture", "WojakCulture" });
        Topographies.TopographiesBaseGame.UnionWith(new[] { "Wetlands", "Flatlands", "Mountains", "Farmlands" });
        Vegetations.VegetationsBaseGame.UnionWith(new[] { "Forest", "Plains", "Desert" });
        Climates.ClimatesBaseGame.UnionWith(new[] { "continental", "cold_arid", "Mediterranean" });
        RawMaterials.RawMaterialsBaseGame.UnionWith(new[] { "Iron", "Wheat", "Coal", "Gold" });
        PopTypes.PopTypesBaseGame.UnionWith(new[] { "Noble", "Peasant", "Artisans", "Clerics" });
    }

   
}

// ----------------------------- generic -----------------------------
public abstract class CacheItemBase
{
    public abstract HashSet<string> GetCombined();
}
public class ReligionC : CacheItemBase
{
    public HashSet<string> ReligionsModded = new HashSet<string>();
    public HashSet<string> ReligionsBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(ReligionsBaseGame);
        combined.UnionWith(ReligionsModded);
        return combined;
    }   
}   
public class CulturesC : CacheItemBase
{
    public HashSet<string> CulturesModded = new HashSet<string>();
    public HashSet<string> CulturesBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(CulturesBaseGame);
        combined.UnionWith(CulturesModded);
        return combined;
    }
}
// ----------------------------- provinces -----------------------------
public class TopographyC : CacheItemBase
{
    public HashSet<string> TopographiesModded = new HashSet<string>();
    public HashSet<string> TopographiesBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(TopographiesBaseGame);
        combined.UnionWith(TopographiesModded);
        return combined;
    }
}
public class VegetationC : CacheItemBase
{
    public HashSet<string> VegetationsModded = new HashSet<string>();
    public HashSet<string> VegetationsBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(VegetationsBaseGame);
        combined.UnionWith(VegetationsModded);
        return combined;
    }
}

public class ClimateC : CacheItemBase
{
    public HashSet<string> ClimatesModded = new HashSet<string>();
    public HashSet<string> ClimatesBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(ClimatesBaseGame);
        combined.UnionWith(ClimatesModded);
        return combined;
    }
}
public class RawMaterialsC : CacheItemBase
{
    public HashSet<string> RawMaterialsModded = new HashSet<string>();
    public HashSet<string> RawMaterialsBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(RawMaterialsBaseGame);
        combined.UnionWith(RawMaterialsModded);
        return combined;
    }
}

// ----------------------------- pops -----------------------------
public class PopTypesC : CacheItemBase
{
    public HashSet<string> PopTypesModded = new HashSet<string>();
    public HashSet<string> PopTypesBaseGame = new HashSet<string>();
    
    public override HashSet<string> GetCombined()
    {
        HashSet<string> combined = new HashSet<string>(PopTypesBaseGame);
        combined.UnionWith(PopTypesModded);
        return combined;
    }
}