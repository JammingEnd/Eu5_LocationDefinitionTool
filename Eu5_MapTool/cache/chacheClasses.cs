using System.Collections.Generic;
using Eu5_MapTool.Services;

namespace Eu5_MapTool.cache;
public class Cache
{
    public readonly ReligionC Religions;
    public readonly CulturesC Cultures;
    public readonly TopographyC Topographies;
    public readonly VegetationC Vegetations;
    public readonly ClimateC Climates;
    public readonly RawMaterialsC RawMaterials;
    public readonly PopTypesC PopTypes;
    
    public Cache(ReligionC reli_c, CulturesC cult_c, TopographyC topo_c, VegetationC vege_c, ClimateC clime_c, RawMaterialsC mats_c, PopTypesC pops_c)
    {
        //TODO: use the StorageService to load cache from disk
        Religions = reli_c;
        Cultures = cult_c;
        Topographies = topo_c;
        Vegetations = vege_c;
        Climates = clime_c;
        RawMaterials = mats_c;
        PopTypes = pops_c;

    }

   
}
public abstract class CacheItemBase
{
    public abstract HashSet<string> GetCombined();
}

public class CombinedCacheItem : CacheItemBase
{
    protected readonly HashSet<string> BaseGame;
    protected readonly HashSet<string> Modded;

    public CombinedCacheItem(HashSet<string> baseGame, HashSet<string> modded)
    {
        BaseGame = baseGame;
        Modded = modded;
    }

    public override HashSet<string> GetCombined()
    {
        var combined = new HashSet<string>(BaseGame);
        combined.UnionWith(Modded);
        return combined;
    }
}

public class ReligionC : CombinedCacheItem
{
    public ReligionC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}

public class CulturesC : CombinedCacheItem
{
    public CulturesC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}

public class TopographyC : CombinedCacheItem
{
    public TopographyC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}

public class VegetationC : CombinedCacheItem
{
    public VegetationC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}

public class ClimateC : CombinedCacheItem
{
    public ClimateC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}

public class RawMaterialsC : CombinedCacheItem
{
    public RawMaterialsC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}

public class PopTypesC : CombinedCacheItem
{
    public PopTypesC(HashSet<string> baseGame, HashSet<string> modded) : base(baseGame, modded) { }
}