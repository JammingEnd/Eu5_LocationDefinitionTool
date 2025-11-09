namespace Eu5_MapTool.Services.Repository;

/// <summary>
/// Represents the state of an entity within a Unit of Work.
/// </summary>
public enum EntityState
{
    /// <summary>
    /// Entity has not been modified.
    /// </summary>
    Unchanged,

    /// <summary>
    /// Entity has been added (new entity).
    /// </summary>
    Added,

    /// <summary>
    /// Entity has been modified.
    /// </summary>
    Modified,

    /// <summary>
    /// Entity has been marked for deletion.
    /// </summary>
    Deleted
}
